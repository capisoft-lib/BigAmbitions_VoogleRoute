# Voogle Route: fixing problems after the LIB BA Unified UI update

Voogle Route now uses two separate shared libraries. If Voogle Route disappeared,
its panel no longer opens, or Big Ambitions says that the save was made with mods
that are no longer present, check the complete installation below.

## Required Workshop items

Subscribe to all three items:

1. [Voogle Route](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471)
2. [LIB BA Unified UI](https://steamcommunity.com/workshop/filedetails/?id=3790426259)
3. [LIB BA Player Location](https://steamcommunity.com/workshop/filedetails/?id=3741773276)

`LIB BA Unified UI` does not add gameplay by itself. It supplies Voogle Route's
panels, buttons, lists and mod options. `LIB BA Player Location` supplies the
player position and movement state. Voogle Route needs both libraries to load.

## Normal installation or update

1. Close Big Ambitions completely.
2. Subscribe to all three Workshop items above.
3. In Steam, wait until every Big Ambitions Workshop download has finished.
4. Start Big Ambitions and open **Mods**.
5. Set **LIB BA Player Location**, **LIB BA Unified UI**, and **Voogle Route** to
   **MOD ACTIVE**.
6. Close Big Ambitions completely, then start it again. This restart is required
   after installing or updating a shared library.
7. Load the save, open Voogle Maps, set a destination and confirm that the
   **VOOGLE ROUTE** panel appears.

If the save warning appeared because a dependency was missing, do not disable
Voogle Route in that save as a workaround. Exit, repair the three-item
installation, restart the game, and load the save again.

## If all three items are active but Voogle Route still does not load

Perform a clean Workshop refresh:

1. Close Big Ambitions.
2. Unsubscribe from Voogle Route and both libraries.
3. Wait for Steam to finish the Workshop update/removal.
4. Subscribe to **LIB BA Player Location** and **LIB BA Unified UI** first, then
   subscribe to **Voogle Route**.
5. Wait for all three downloads to finish.
6. Start the game, mark all three as **MOD ACTIVE**, close the game completely,
   and start it once more.

If you have used the Big Ambitions Modding SDK, also check that an older local
copy is not loading alongside the Workshop version. Remove or move only the
duplicate `VoogleRoute`, `LIB_BaUnifiedUI`, and `LIB_BaPlayerLocation` folders
from the game's `ModsLocal` directory, then restart. Regular Workshop users
normally do not have these local copies.

## Still not working?

Open a [Voogle Route GitHub issue](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/issues)
and include:

- the exact symptom or error message;
- the Big Ambitions version/branch;
- a screenshot of the Mods screen showing all three items as **MOD ACTIVE**;
- `Player.log`, found at
  `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\Player.log`.

Review the log before posting it publicly because it can contain local file
paths or a Windows user name.

---

# Français : résoudre les problèmes depuis l'ajout de LIB BA Unified UI

Voogle Route utilise maintenant deux bibliothèques partagées séparées. Si le
mod a disparu, si son panneau ne s'affiche plus, ou si Big Ambitions indique que
la sauvegarde utilise des mods qui ne sont plus présents, vérifiez l'installation
complète ci-dessous.

## Éléments Workshop requis

Abonnez-vous aux trois éléments :

1. [Voogle Route](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471)
2. [LIB BA Unified UI](https://steamcommunity.com/workshop/filedetails/?id=3790426259)
3. [LIB BA Player Location](https://steamcommunity.com/workshop/filedetails/?id=3741773276)

`LIB BA Unified UI` n'ajoute aucun gameplay à elle seule : elle fournit les
panneaux, boutons, listes et options de Voogle Route. `LIB BA Player Location`
fournit la position et l'état de déplacement du joueur. Les deux bibliothèques
sont nécessaires au chargement de Voogle Route.

## Installation ou mise à jour normale

1. Fermez complètement Big Ambitions.
2. Abonnez-vous aux trois éléments Workshop ci-dessus.
3. Dans Steam, attendez la fin de tous les téléchargements Workshop de Big
   Ambitions.
4. Lancez Big Ambitions et ouvrez **Mods**.
5. Placez **LIB BA Player Location**, **LIB BA Unified UI** et **Voogle Route**
   sur **MOD ACTIVE**.
6. Fermez complètement Big Ambitions, puis relancez-le. Ce redémarrage est
   nécessaire après l'installation ou la mise à jour d'une bibliothèque
   partagée.
7. Chargez la sauvegarde, définissez une destination dans Voogle Maps et
   vérifiez que le panneau **VOOGLE ROUTE** apparaît.

Si l'avertissement de sauvegarde est apparu parce qu'une dépendance manquait,
ne désactivez pas Voogle Route dans cette sauvegarde pour contourner le problème.
Quittez, réparez l'installation des trois éléments, redémarrez le jeu et chargez
à nouveau la sauvegarde.

## Les trois éléments sont actifs mais Voogle Route ne charge toujours pas

Effectuez un rafraîchissement propre du Workshop :

1. Fermez Big Ambitions.
2. Désabonnez-vous de Voogle Route et des deux bibliothèques.
3. Attendez que Steam termine la mise à jour/suppression Workshop.
4. Réabonnez-vous d'abord à **LIB BA Player Location** et à **LIB BA Unified UI**,
   puis à **Voogle Route**.
5. Attendez la fin des trois téléchargements.
6. Lancez le jeu, placez les trois éléments sur **MOD ACTIVE**, fermez
   complètement le jeu, puis relancez-le une dernière fois.

Si vous avez utilisé le SDK de modding Big Ambitions, vérifiez également qu'une
ancienne copie locale ne se charge pas en même temps que la version Workshop.
Retirez ou déplacez uniquement les dossiers `VoogleRoute`, `LIB_BaUnifiedUI` et
`LIB_BaPlayerLocation` en double dans le dossier `ModsLocal`, puis redémarrez.
Les utilisateurs classiques du Workshop n'ont normalement pas ces copies
locales.

## Le problème persiste ?

Ouvrez un [ticket GitHub Voogle Route](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/issues)
avec :

- le symptôme ou message d'erreur exact ;
- la version/branche de Big Ambitions ;
- une capture de l'écran Mods montrant les trois éléments sur **MOD ACTIVE** ;
- le fichier `Player.log`, disponible dans
  `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\Player.log`.

Relisez le journal avant de le publier : il peut contenir des chemins locaux ou
le nom du compte Windows.
