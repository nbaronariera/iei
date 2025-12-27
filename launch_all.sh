trap "kill 0" EXIT
dotnet watch run --project ./SolutionIEI/ProyectoAPIBusqueda &
dotnet watch run --project ./SolutionIEI/ProyectoAPICarga &
dotnet watch run --project ./SolutionIEI/ProyectoAPICat &
dotnet watch run --project ./SolutionIEI/ProyectoAPICV &
dotnet watch run --project ./SolutionIEI/ProyectoAPIGal &
wait