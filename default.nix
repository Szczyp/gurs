{ pkgs ? import ./nixpkgs.nix }:
with pkgs;
let
  drv = {};

  shell = pkgs.mkShell {
    buildInputs = [ dotnetCorePackages.sdk_3_1 vscode omnisharp-roslyn ];
  };
in
drv // { inherit shell; }
