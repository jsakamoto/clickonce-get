### Prerequires

Installed "Git for Windows", and set "PATH" environment to `C:\Program Files\Git\usr\bin`.

### Step 1

Generate private key and public key for SSH format.

```shell
> ssh-keygen.exe -f id_rsa
```

This command generate `id_rsa` file (RSA 2048 bits private key with PEM format)
and `id_rsa.pub` file (publick key with SSH format).

### Step 2

Generate self signed certificate from private key.

```shell
> openssl req -new -x509 -key .\id_rsa -out .\id_rsa.cer -days 36500
```

This command generate `id_rsa.cer` file (X.509 certificate with PEM format).

### Mission

Check the equality of public keys from `id_rsa.pub` file and `id_rsa.cer` file.