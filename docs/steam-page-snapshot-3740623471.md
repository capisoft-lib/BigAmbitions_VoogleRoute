# Steam Workshop page snapshot — Voogle Route

**URL:** https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471  
**Saved:** 2026-06-17  
**Purpose:** Archive of what automated tools see when fetching the page (not a logged-in browser session).

---

## A. Contenu injecté dans le chat (web search, message précédent)

Source : résultat web automatique joint à la conversation quand l’URL a été partagée.

```
Sign in  Store 
...
Big Ambitions > Workshop > - Capitaine -'s Workshop 

 This item has been removed from the community because it violates Steam Community & Content Guidelines. It is only visible to you. If you believe your item has been removed by mistake, please contact Steam Support. 

 This item is incompatible with Big Ambitions. Please see the instructions page for reasons why this item might not work within Big Ambitions. 

 Not enough ratings 

Voogle Route
...
Posted: Jun 7 @ 2:22pm
Updated: Jun 15 @ 12:10pm
...
```

### Où trouver les textes de visibilité (section A)

| Texte | Dans le bloc ci-dessus |
|-------|-------------------------|
| `This item has been removed from the community because it violates Steam Community & Content Guidelines. It is only visible to you.` | Paragraphe après `Big Ambitions > Workshop > ...` |
| `This item is incompatible with Big Ambitions.` | Paragraphe suivant |

---

## B. Fetch HTTP du 2026-06-17 (outil Cursor, sans session Steam utilisateur)

Même URL, récupération automatisée non authentifiée.

```
Steam Workshop::Voogle Route

Big Ambitions
...
### Big Ambitions

This item has been removed from the community because it violates Steam Community & Content Guidelines. It is only visible to you. If you believe your item has been removed by mistake, please contact Steam Support.

This item is incompatible with Big Ambitions. Please see the instructions page for reasons why this item might not work within Big Ambitions.

Not enough ratings

Voogle Route
...
File Size: 3.576 MB
Posted: 7 Jun @ 2:22pm
Updated: 15 Jun @ 12:10pm
13 Change Notes
...
Description (extrait début):
Voogle Route extends Voogle Maps...
What's new in 0.11.8
...
16 Comments
...
```

### Où trouver les textes de visibilité (section B)

| Texte | Position dans la section B |
|-------|----------------------------|
| `This item has been removed from the community...` | Juste après `### Big Ambitions`, avant `Not enough ratings` |
| `This item is incompatible with Big Ambitions...` | Ligne suivante |

---

## Note importante

Ces snapshots viennent d’**outils automatisés sans connexion à ton compte Steam**.

Si tu ne vois **pas** ces bandeaux dans ton navigateur connecté en tant qu’auteur, c’est cohérent : Steam affiche souvent un contenu différent selon :

- connecté / déconnecté
- auteur / abonné / visiteur anonyme
- client Steam vs navigateur

Ne pas utiliser ce fichier comme preuve du statut réel de l’item sans vérifier ta propre session.

---

## C. Capture navigateur — auteur connecté (2026-06-17)

Source : capture d’écran Steam Workshop en session auteur (français), même URL `3740623471`.

**Observé sur la capture :**

- Page normale : titre **Voogle Route**, bouton **+ S'abonner**, stats visiteurs/abonnés
- **Aucun** bandeau « removed from the community » / « only visible to you »
- **Aucun** message « incompatible with Big Ambitions »
- Métadonnées visibles : **3,576 MB**, publié **7 juin**, mis à jour **15 juin**, **13 notes de changement**
- Élément requis : **LIB_BaPlayerLocation**

**Conclusion :** les sections A et B (fetch automatisé / non authentifié) ne reflètent **pas** ce que l’auteur voit connecté. Écarter la piste « item masqué / retiré » pour le diagnostic upload in-game.
