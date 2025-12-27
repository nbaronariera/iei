
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
  ];

  shellHook = ''
    # OJO: Aquí tenías un error, apuntabas al sdk_8. Lo cambio a sdk_6
    export DOTNET_ROOT="${pkgs.dotnet-sdk_6}"

    export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

    echo "⚠️  ATENCIÓN: Usando versión EOL de .NET 6"
    dotnet --info
  '';
}
