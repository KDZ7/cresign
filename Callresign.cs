
using Renci.SshNet;

namespace Callresign.Tools
{
	public class Param
	{
		private string[] _params;
		public Param(params string[] _params)
		{
			this._params = _params;
		}
		/// fonction de recuperation des arguments d'une parametre
		public string param_arg(string param, string[] args)
		{
			string arg = "";
			for (int i = 0; i < args.Length; i++)
				if (args[i] == param)
					for (int j = i + 1; j < args.Length; j++)
					{
						if (args[i] != args[j])
							foreach (string _param in _params)
								if (args[j] == _param)
									goto exit;
						arg += args[j];
					}
				exit:
			return arg;
		}

		public void param_help()
		{
			System.Console.WriteLine
(@"

exemple:

cresign -s user@192.168.1.18 -port 22 -password ""mypassword"" -w ""Desktop/work_dir"" -c ""Apple Development: Name (XXXXXXXXXX)"" -p XXXXXXXXX.mobileprovision -i myfile.ipa

exemple en cas de présence de nouvelles couches à resigner:

cresign -s user@192.168.1.18 -port 22 -password ""mypassword"" -w ""Desktop/work_dir"" -c ""Apple Development: Name (XXXXXXXXXX)"" -p XXXXXXXXX.mobileprovision -i myfile.ipa -e '*.appex','*.dylib'

-s:        user@ip pour la communication SSH
-port:     port du user@ip pour la communication SSH (22 par défaut)
-password: mot du passe de la machine user@ip
-w:        dossier de travail de <cresign> sur la machine user@ip
-c:        nom du certificat
-p:        profil d'approvisionnement
-i:        nom du fichier .ipa
-e:        ajouter d'autres nouvelles couches à resigner (optionnel)

");

		}
	}
}

namespace Callresign.Ssh
{
	public delegate Task<bool> Callback();
	internal interface ICresign
	{
		bool login(in string user, in string ip, in short port, in string password, in string work_dir);
		bool logout();
		Task<bool> send(string filename);
		Task<bool> receive(string filename);
		Task<bool> resign(string filename_ipa, string certificate, string prov_profile, string password, string other_exts, Callback callback);
		bool clear(string filename);
	}

	public class Cresign : ICresign
	{
		private ConnectionInfo? connectionInfo = null;
		private SshClient? sshClient = null;
		private SftpClient? sftpClient = null;
		private string? work_dir;
		public bool login(in string user, in string ip, in short port, in string password, in string work_dir)
		{
			try
			{
				connectionInfo = new ConnectionInfo(ip, port, user, new PasswordAuthenticationMethod(user, password));
				sshClient = new SshClient(connectionInfo);
				sshClient.Connect();
				sftpClient = new SftpClient(connectionInfo);
				sftpClient.Connect();
				this.work_dir = work_dir;
				sftpClient.ChangeDirectory(this.work_dir);
				System.Console.WriteLine("\n\n\t connection successful ");
				return true;
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t connection error: " + exception.Message);
				return false;
			}
		}

		public bool logout()
		{
			try
			{
				if (sftpClient != null)
					if (sftpClient.IsConnected)
						sftpClient.Disconnect();
				if (sshClient != null)
					if (sshClient.IsConnected)
						sshClient.Disconnect();
				System.Console.WriteLine("\n\n\t disconnected ");
				return true;
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t disconnection error: " + exception.Message);
				return false;
			}
		}

		public async Task<bool> send(string filename)
		{
			if (sftpClient == null)
				return false;
			try
			{
				using (var filestream = new FileStream(filename, System.IO.FileMode.Open))
				{

					var asyncResult = sftpClient.BeginUploadFile(filestream, filename);
					while (!asyncResult.IsCompleted)
					{
						await Task.Delay(100);
						System.Console.Write($"\t uploading ... {(sftpClient.GetAttributes(filename).Size * 100 / filestream.Length)}%\r ");
					}
					sftpClient.EndUploadFile(asyncResult);

					if (filestream.Length != sftpClient.GetAttributes(filename).Size)
						throw new Exception("\n the ipa file was not completely uploaded ");
					System.Console.WriteLine("\n The file was uploaded successfully ");

					return true;

				}
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t upload error: " + exception.Message);
				return false;
			}
		}
		public async Task<bool> receive(string filename)
		{
			if (sftpClient == null)
				return false;

			try
			{
				using (var filestream = new FileStream(filename, System.IO.FileMode.Create))
				{

					var asyncResult = sftpClient.BeginDownloadFile(filename, filestream);
					while (!asyncResult.IsCompleted)
					{
						await Task.Delay(100);
						System.Console.Write($"\t downloading ... {(filestream.Length * 100 / sftpClient.GetAttributes(filename).Size)}%\r ");
					}
					sftpClient.EndDownloadFile(asyncResult);

					if (filestream.Length != sftpClient.GetAttributes(filename).Size)
						throw new Exception("\n the ipa file was not completely downloaded ");
					System.Console.WriteLine("\n The file was download successfully ");

					return true;
				}
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t download error: " + exception.Message);
				return false;
			}
		}
		public async Task<bool> resign(string filename_ipa, string certificate, string prov_profile, string password, string other_exts, Callback callback)
		{
			if (sshClient == null)
				return false;

			try
			{
				var cmd = sshClient.CreateCommand(
								$"cd \"{this.work_dir}\" && " +
								$"sh resign.sh -c \"{certificate}\" -p \"{prov_profile}\" -password \"{password}\" -i \"{filename_ipa}\" -e \"{other_exts}\""
								);

				var asyncResult = cmd.BeginExecute();
				using (var outStream = new StreamReader(cmd.OutputStream))
				using (var errorStream = new StreamReader(cmd.ExtendedOutputStream))
				{
					System.Console.WriteLine($"\n\n\t\t On the remote machine {sshClient.ConnectionInfo.Username}@{sshClient.ConnectionInfo.Host}");
					while (!asyncResult.IsCompleted)
					{
						while (!outStream.EndOfStream)
							System.Console.WriteLine(await outStream.ReadLineAsync());
						while (!errorStream.EndOfStream)
						{
							string? line = await errorStream.ReadLineAsync();
							System.Console.WriteLine(line);
							if (!string.IsNullOrEmpty(line))
								if (!line.Contains("replacing existing signature"))
									throw new Exception(": check < cresign > arguments or < resign.sh > configurations");

						}
					}
					cmd.EndExecute(asyncResult);
					return await callback();
				}
			}

			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t error on remote terminal" + exception.Message);
				clear(filename_ipa);
				return false;
			}
		}
		public bool clear(string filename)
		{
			if (sshClient == null)
				return false;
			try
			{
				sshClient.CreateCommand($"cd \"{this.work_dir}\" && rm -rf \"{filename}\"").Execute();
				return true;
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("\n\n\t remote machine cleanup failed: " + exception.Message);
				return false;
			}

		}

	}
}