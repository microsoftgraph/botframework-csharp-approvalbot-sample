---
page_type: sample
products:
- office-outlook
- office-onedrive
- ms-graph
languages:
- csharp
description: « Un exemple de robot qui utilise des cartes adaptatives et le kit de développement logiciel (SDK) .NET Graph pour envoyer des messages actionnables demandant l’approbation de publier des fichiers sur OneDrive. »
extensions:
  contentType: samples
  technologies:
  - Microsoft Graph
  - Microsoft Bot Framework
  services:
  - Outlook
  - OneDrive
  createdDate: 4/23/2018 12:12:07 PM
---
# Exemple de Robot d’approbation

## Exécution locale

Effectuez ces étapes pour activer l’exécution locale du robot pour le débogage.

### Conditions préalables

- [ngrok](https://ngrok.com/)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
- [Émulateur Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- Visual Studio 2017

### Inscrire l’application

1. Ouvrez un navigateur et accédez au [Centre d’administration Azure Active Directory](https://aad.portal.azure.com). Connectez-vous en utilisant un **compte professionnel ou scolaire**.

1. Sélectionnez **Azure Active Directory** dans le volet de navigation gauche, puis sélectionnez **Inscriptions d’applications (préversion)** sous **Gérer**.

    ![Capture d’écran des inscriptions d’applications ](readme-images/aad-portal-app-registrations.png)

1. Sélectionnez **Nouvelle inscription**. Sur la page **Inscrire une application**, définissez les valeurs comme suit :

    - Donnez un **Nom** favori, par ex. `Robot d’approbation`.
    - Définissez **Types de comptes pris en charge** sur **Comptes figurant dans un annuaire organisationnel**.
    - Sous **URI de redirection**, définissez la première liste déroulante à `Web` et la valeur sur `http://localhost:3979/callback`.

    ![Capture d’écran de la page Inscrire une application](readme-images/aad-register-an-app.PNG)

1. Choisissez **Inscrire**. Sur la page **Robot d’approbation**, copiez la valeur de l’**ID (client) d’application** et enregistrez-la, elle sera utilisée à l’étape suivante.

    ![Une capture d’écran de l’ID d’application de la nouvelle inscription d'application](readme-images/aad-application-id.PNG)

1. Sélectionnez **Certificats et secrets** sous **Gérer**. Sélectionnez le bouton **Nouvelle clé secrète client**. Entrez une valeur dans la **Description**, sélectionnez l'une des options pour **Expire le**, puis choisissez **Ajouter**.

    ![Une capture d’écran de la boîte de dialogue Ajouter une clé secrète client](readme-images/aad-new-client-secret.png)

1. Copiez la valeur due la clé secrète client avant de quitter cette page. Vous en aurez besoin à l’étape suivante.

    > [!IMPORTANT]
    > Cette clé secrète client n’apparaîtra plus, aussi veillez à la copier maintenant.

    ![Une capture d’écran de la clé secrète client nouvellement ajoutée](readme-images/aad-copy-client-secret.png)

### Configurer le proxy ngrok

Vous devez exposer un point de terminaison public HTTPs pour recevoir des notifications de Bot Framework Emulator. Pendant le test, vous pouvez utiliser ngrok pour autoriser temporairement les messages de Bot Framework Emulator à passer par un tunnel vers un port *localhost* sur votre ordinateur.

Vous pouvez utiliser l’interface web ngrok ([http://127.0.0.1:4040](http://127.0.0.1:4040)) pour inspecter le trafic HTTP qui traverse le tunnel. Pour en savoir plus sur l’utilisation de ngrok, consultez le [site web ngrok](https://ngrok.com/).


1. [Télécharger ngrok](https://ngrok.com/download) pour Windows.

1. Décompressez le paquet et exécutez ngrok.exe.

1. Exécutez la ligne de commande suivante dans la console ngrok :

    ```Shell
    ngrok http 3979 --host-header=localhost:3979
    ```

    ![Exemple de commande à exécuter dans la console ngrok](readme-images/ngrok1.PNG)

1. Copiez l’URL HTTPs affichée dans la console. Vous l’utiliserez pour configurer `NgrokRootUrl` dans l’exemple.

    ![L’URL HTTPs de transfert dans la console ngrok](readme-images/ngrok2.PNG)

    > **Remarque :** Gardez la console ouverte pendant le test. Si vous la fermez, le tunnel se ferme également et vous devrez générer une nouvelle URL et mettre à jour l’exemple.

### Configurer et exécuter l’exemple

1. Cloner le référentiel localement.
1. Effectuez une copie du fichier ./ApprovalBot/PrivateSettings.example.config dans le même répertoire, puis nommez cette copie `PrivateSettings.config`.
1. Ouvrez ApprovalBot.sln dans Visual Studio, puis ouvrez le fichier **PrivateSettings.config**.

1. Attribuez la valeur de `MicrosoftAppId` à l’ID (client) d’application que vous avez généré à l’étape précédente et attribuez la valeur de `MicrosoftAppPassword` à la clé secrète que vous avez générée par la suite.

1. Collez la valeur ngrok URL HTTPs copiée à l’étape précédente dans la valeur de NgrokRootUrl dans **PrivateSettings.config** et enregistrez vos modifications.

    > **IMPORTANT** : Laissez ngrok en cours d’exécution pendant l’exécution de l’exemple. Si vous arrêtez ngrok et le redémarrez, l’URL de transfert change et vous devez mettre à jour la valeur de `NgrokRootUrl`.

1. Démarrez l’émulateur Azure Cosmos DB. Celui-ci doit être en cours d’exécution avant le démarrage de l’exemple.

1. Appuyez sur F5 pour déboguer l’exemple.

1. Exécutez Bot Framework Emulator. Dans la partie supérieure, là où s’affiche **Entrez l’URL de votre point de terminaison**, entrez `https://localhost:3979/api/messages`.

1. Vous serez invité à entrer l’ID d’application et le mot de passe. Entrez votre ID d’application et votre clé secrète, et laissez le champ **Paramètres régionaux** vide.

    ![](readme-images/configure-emulator.PNG)

1. Cliquez sur **Connecter**.

1. Envoyez `hi` pour confirmer la connexion.

    ![](readme-images/hello-bot.PNG)

## Limitations lors de l’exécution locale

Lors de l’exécution de l’exemple localement, le courrier de demande d’approbation envoyé est un peu différent. Étant donné qu’il n’est pas en cours d’exécution sur un domaine confirmé et inscrit, nous devons envoyer le message vers et à partir du même compte. Cela veut dire qu’au moment de demander une approbation, vous devez vous inclure vous-même dans la liste des approbateurs.

Vous pouvez inclure d’autres approbateurs, mais le message qu’ils recevront n’affichera pas la carte adaptative. Connectez-vous à votre propre boîte aux lettres avec Outlook pour tester la carte adaptative.

## Contribution

Ce projet autorise les contributions et les suggestions.
Pour la plupart des contributions, vous devez accepter le contrat de licence de contributeur (CLA, Contributor License Agreement) stipulant que vous êtes en mesure, et que vous vous y engagez, de nous accorder les droits d’utiliser votre contribution.
Pour plus d’informations, visitez https://cla.microsoft.com.

Lorsque vous soumettez une requête de tirage, un robot CLA détermine automatiquement si vous devez fournir un CLA et si vous devez remplir la requête de tirage appropriée (par exemple, étiquette, commentaire).
Suivez simplement les instructions données par le robot.
Vous ne devrez le faire qu’une seule fois au sein de tous les référentiels à l’aide du CLA.

Ce projet a adopté le [code de conduite Open Source de Microsoft](https://opensource.microsoft.com/codeofconduct/).
Pour en savoir plus, reportez-vous à la [FAQ relative au code de conduite](https://opensource.microsoft.com/codeofconduct/faq/)
ou contactez [opencode@microsoft.com](mailto:opencode@microsoft.com) pour toute question ou tout commentaire.

## Copyright

Copyright (c) 2019 Microsoft. Tous droits réservés.
