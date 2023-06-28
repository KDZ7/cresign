# cresign
cresign -> resign.sh (IPA Signature)

Cresign is a software developed in C# that allows communication with a shell script called "resign.sh" (link: https://github.com/KDZ7/signature-ipa). 
The "resign.sh" script should be placed in a specific folder on a remote Mac machine. Cresign facilitates the automatic launching of "resign.sh" by calling it from any Windows/Linux machine.

The usefulness of using Cresign lies in its portability on Windows/Linux and its ability to easily launch an IPA resigning process on a remote Mac machine. It is particularly useful for automating the IPA resigning process.

Usage conditions:

  - Have a powered-on Mac and another machine running Windows/Linux.
  - Have a properly configured Apple developer account on the Mac, and the provisioning profiles you want to use for resigning should be placed in the same specific folder as the "resign.sh" file (for example: Desktop/work_dir) on the Mac.
  - SSH communication between the remote machine and the Mac must be functioning correctly.

If these conditions are met, the automation of the resigning process via SSH should work. For example, to resign the file "myfile.ipa", simply enter the following command:

	$ cresign -s user@192.168.1.18 -port 22 -password "mypassword" -w "Desktop/work_dir" -c "Apple Development: Name (XXXXXXXXXX)" -p XXXXXXXXX.mobileprovision -i myfile.ipa

Steps:

> Cresign sends the IPA file to the Mac ---> The Mac automatically initiates the resigning process using resign.sh ---> Cresign retrieves the resigned IPA file from the machine by downloading it under the name "myfile-resigned.ipa".

