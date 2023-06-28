using Callresign.Tools;
using Callresign.Ssh;

class Program
{
  short port = 22;
  string user = "";
  string ip = "";
  string password = "";
  string work_dir = "";
  string certificate = "";
  string prov_profile = "";
  string filename_ipa = "";
  string other_exts = "";

  /// Processus de récupération des arguments des paramètres
  Program(string[] args)
  {
    Param param = new Param("-c", "-e", "-h", "-i", "-p", "-s", "-w", "-password", "-port");
    if (args.Contains("-h"))
    {
      param.param_help();
      Environment.Exit(0);
    }

    {
      if (!short.TryParse(param.param_arg("-port", args), out port))
        port = 22;
      string str = param.param_arg("-s", args);
      if (!string.IsNullOrEmpty(str))
      {
        user = str.Split("@")[0];
        ip = str.Split("@")[1];
      }
    }

    password = param.param_arg("-password", args);
    work_dir = param.param_arg("-w", args);
    certificate = param.param_arg("-c", args);
    prov_profile = param.param_arg("-p", args);
    filename_ipa = param.param_arg("-i", args);
    foreach (string ext in param.param_arg("-e", args).Split(","))
      other_exts += "\"" + ext + "\",";

    System.Console.WriteLine("\n\n user: " + user);
    System.Console.WriteLine(" ip: " + ip);
    System.Console.WriteLine(" password: " + password);
    System.Console.WriteLine(" port: " + port);
    System.Console.WriteLine(" work directory: " + work_dir);
    System.Console.WriteLine(" certificate Apple: " + certificate);
    System.Console.WriteLine(" provisioning profile: " + prov_profile);
    System.Console.WriteLine(" ipa file: " + filename_ipa);
    System.Console.WriteLine(" other extensions: " + other_exts);
  }

  static async Task Main(string[] args)
  {
    Program program = new Program(args);
    Cresign cresign = new Cresign();

    if (!cresign.login(
        program.user,
        program.ip,
        program.port,
        program.password,
        program.work_dir
        ))
      return;


    if (!await cresign.send(program.filename_ipa))
      return;

    await cresign.resign(
        program.filename_ipa,
        program.certificate,
        program.prov_profile,
        program.password,
        program.other_exts,
        new Callback(() => cresign.receive(program.filename_ipa.Replace(".ipa", "-resigned.ipa").Replace(" ", "")))
    );

    cresign.clear(program.filename_ipa.Replace(".ipa", "-resigned.ipa").Replace(" ", ""));

    cresign.logout();
  }

}
