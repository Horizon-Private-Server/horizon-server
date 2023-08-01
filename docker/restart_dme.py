import subprocess
from datetime import datetime
import time

time.sleep(60)

def run_bash_command(command):
    process = subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    stdout, stderr = process.communicate()
    return_code = process.returncode

    if return_code == 0:
        return stdout.strip()
    else:
        raise RuntimeError("Command execution failed with return code {}. Error: {}".format(return_code, stderr.strip()))

def restart_dme():
    command = 'cd dme && dotnet Server.Dme.dll &'
    print command
    subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

# Example usage
while True:
    command_output = run_bash_command("ps aux")
    dme_running = "dotnet Server.Dme.dll" in command_output
    print "Dme running: " + str(dme_running)
    print datetime.now()
    print "Command output:"
    print command_output

    if not dme_running:
        print("Restarting DME!")
        restart_dme()

    time.sleep(60)


