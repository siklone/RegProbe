$ErrorActionPreference = 'Stop'

# Launch shutdown asynchronously so vmrun can return success before the
# guest session is torn down by the reboot.
Start-Process -FilePath "$env:SystemRoot\System32\shutdown.exe" -ArgumentList '/r', '/t', '0', '/f' -WindowStyle Hidden
Start-Sleep -Milliseconds 250
