# PFG: Agents per la prevenció i mitigació d'incendis

Ordre d'execució:

1- Crear un EmptyObject i posar-hi l'script de FireMapsPreparation

2- Crear un EmptyObject i posar-hi l'script d'EnvironmentManager

3- Crear un EmptyObject i posari l'script de l'Agent1
4- Al FireMapsPreparation, introduir el fitxer del tipus de sòl a "input_vegetation_map", introduir el "height_map", el plane object (crear-ne un) i el material del plane on es faran les simulacions.
5- A l'EnviromentManager, introduir els camps necessaris (similars al del FireMapsPreparation), introduir el nom del fitxer amb els mappings i carregar ("Load mappings") o calcular els mappings ("Calculate color mappings"). Si es volen calcular de nou, empleneu les dades ("Cathegory", "Burn Priority" i "Expand Coefficient") a la llista desplegable "Mappings" i tot seguit clicar "Save mappings".
6- Al FireMapsPreparation, introduir el nombre de jocs de prova que es volen generar que contene condicions normals. Clicar "Pregenerate fire simulations".
7- A l'EnviromentManager, clicar "Preprocess map".
8- Ja es pot iniciar l'entrenament
