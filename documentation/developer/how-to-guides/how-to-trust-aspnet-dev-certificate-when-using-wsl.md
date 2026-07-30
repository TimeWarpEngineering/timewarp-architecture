# How To Trust ASP.NET Core Development Certificates When Using WSL

## Overview
This guide documents the end-to-end process for repairing and trusting the ASP.NET Core HTTPS development certificate when the .NET workload runs inside WSL (Linux) but browsers and debugging tools run on Windows. The workflow covers recreating the certificate, importing it into Linux trust stores, and registering it with the Windows certificate store so Microsoft Edge recognizes localhost endpoints as secure.

## Prerequisites
- WSL distribution with `dotnet`, `openssl`, and `sudo` available.
- `libnss3-tools` installed if Chromium- or Firefox-based browsers run inside the WSL environment.
- Access to the Windows host where Edge (or other desktop browsers) will trust the certificate.

## 1. Reset and Recreate the Dev Certificate (WSL)
Run the following commands inside the WSL distribution to remove the old certificate, create a fresh one, and verify it exists.

```bash
# Remove all existing HTTPS development certificates
dotnet dev-certs https --clean

# Create a new certificate and attempt to trust it for the current user
dotnet dev-certs https --trust

# Confirm the certificate was created
dotnet dev-certs https --check
```

> Note: On Linux `--trust` may not propagate to system stores; subsequent steps handle that.

## 2. Export The Certificate From WSL
Export the certificate (including the private key) to a `.pfx` so it can be installed elsewhere. Use a temporary password and plan to delete the exported files after import.

```bash
# Export the certificate (update the password value as desired)
dotnet dev-certs https \
  --export-path /tmp/aspnet-dev-cert.pfx \
  --password "timeWarpTemp!"
```

Confirm the file exists:

```bash
ls -l /tmp/aspnet-dev-cert.pfx
```

## 3. Convert To PEM/DER For Linux Stores
Extract a PEM-encoded version and a DER `.cer` for downstream imports.

```bash
# Extract certificate (no private key) to PEM
openssl pkcs12 \
  -in /tmp/aspnet-dev-cert.pfx \
  -clcerts -nokeys \
  -out /tmp/aspnet-dev-cert.pem \
  -passin pass:timeWarpTemp!

# Produce a DER-formatted .cer for Windows import (optional but handy)
openssl x509 \
  -in /tmp/aspnet-dev-cert.pem \
  -outform der \
  -out /tmp/aspnet-dev-cert.cer
```

## 4. Trust The Certificate On Linux (WSL)
Install the PEM into the system CA directory and refresh the certificate store.

```bash
sudo cp /tmp/aspnet-dev-cert.pem /usr/local/share/ca-certificates/aspnet-dev-cert.crt
sudo update-ca-certificates
```

Warnings about files "not containing exactly one certificate" are common and can be ignored if the summary states that one certificate was added.

If Chromium/Edge/Brave run inside WSL, import the cert into their NSS store as well:

```bash
certutil -d sql:$HOME/.pki/nssdb \
  -A -t "C,," \
  -n "ASP.NET Core HTTPS development certificate" \
  -i /tmp/aspnet-dev-cert.pem
```

For Firefox inside WSL, repeat the `certutil` command against each profile directory (e.g., `~/.mozilla/firefox/<profile>.default-release`).

## 5. Copy Certificate To Windows Host
Move the exported files from WSL to Windows via the mounted drive (replace `<WindowsUser>` accordingly):

```bash
cp /tmp/aspnet-dev-cert.cer /mnt/c/Users/<WindowsUser>/Downloads/
cp /tmp/aspnet-dev-cert.pfx /mnt/c/Users/<WindowsUser>/Downloads/
```

## 6. Trust The Certificate On Windows (Edge/Chrome)
Perform these steps on the Windows desktop:

1. Press `Win + R`, type `mmc`, and press Enter.
2. File → Add/Remove Snap-in → select **Certificates** → Add → choose **Computer account** → Finish → OK.
3. Expand `Certificates (Local Computer)` → `Trusted Root Certification Authorities` → `Certificates`.
4. Delete stale `localhost` entries if present.
5. Right-click `Certificates` → `All Tasks` → `Import...`.
6. Browse to `Downloads\aspnet-dev-cert.cer` (use the `All Files` filter if needed).
7. When prompted for a store, ensure **Trusted Root Certification Authorities** is selected.
8. Complete the wizard and accept the security warning about trusting the certificate.

> Optional: Import the `.pfx` into `Certificates (Local Computer) → Personal` if you also want the private key available in Windows.

Verify the certificate exists by running (in Windows PowerShell):

```powershell
certutil -store root localhost
```

## 7. Restart Services And Browsers
- Restart Aspire or any local web host so it reloads the trusted certificate.
- Restart Microsoft Edge (navigate to `edge://restart`) and browse to `https://localhost:<port>` to confirm the lock icon is green.

## 8. Cleanup
For security, remove the exported certificate files once all imports succeed:

```bash
rm /tmp/aspnet-dev-cert.pfx /tmp/aspnet-dev-cert.pem /tmp/aspnet-dev-cert.cer
```

Also delete the copies placed under the Windows profile after confirming trust (e.g., remove them from `Downloads`).

## Troubleshooting Tips
- If `dotnet dev-certs https --check --trust` still reports "none trusted" inside WSL, rely on `update-ca-certificates` and browser imports instead; the command only reflects current user stores.
- Binding errors such as `Address already in use` during Aspire startup often indicate a stuck host process. Use `pgrep -fl Aspire` / `kill <pid>` inside WSL before restarting.
- Should browsers continue to warn, double-check that the certificate is in the **Local Computer** root store (not just Current User) and that the Subject Alternative Names include `localhost`.
