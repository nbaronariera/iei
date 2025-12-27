
{ pkgs ? import <nixpkgs> {
    config.permittedInsecurePackages = [
      "dotnet-sdk-6.0.428"
    ];
  }
}:

pkgs.mkShell {
  name = "dotnet-env-legacy";

  packages = [
    pkgs.dotnet-sdk_6
    pkgs.dotnet-ef
    pkgs.chromium
    pkgs.chromedriver
  ];

  shellHook = ''
    export DOTNET_ROOT="${pkgs.dotnet-sdk_6}"
    export CHROME_BIN="${pkgs.chromium}/bin/chromium"
    export CHROMEDRIVER_PATH="${pkgs.chromedriver}/bin/chromedriver"
    export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
    dotnet --info
  '';
}
