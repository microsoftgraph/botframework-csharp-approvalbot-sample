---
page_type: sample
products:
- office-outlook
- office-onedrive
- ms-graph
languages:
- csharp
description: "アダプティブ カードと .NET Graph SDK を使用して、OneDrive 上のファイルを解放するために承認を要求するアクション可能メッセージを送信するサンプル ボット"
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
# 承認ボットのサンプル

## ローカルで実行

次の手順に従って、デバッグのためにボットをローカルで実行します。

### 必要条件

- [ngrok](https://ngrok.com/)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
- [Azure Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- Visual Studio 2017

### アプリの登録

1. ブラウザーを開き、[Azure Active Directory 管理センター](https://aad.portal.azure.com)に移動します。**職場または学校アカウント**を使用してログインします。

1. 左側のナビゲーションで **[Azure Active Directory]** を選択し、それから **[管理]** で **[アプリの登録 (プレビュー)]** を選択します。

    ![アプリの登録のスクリーンショット ](readme-images/aad-portal-app-registrations.png)

1. **[新規登録]** を選択します。[**アプリケーションの登録**] ページで、次のように値を設定します。

    - `Approval Bot` のように、優先する [**名前**] を設定します。
    - [**サポートされているアカウントの種類**] を [**組織のディレクトリ内のアカウント**] に設定します。
    - [**リダイレクト URI**] で、最初のドロップダウン リストを [`Web`] に設定し、それから `http://localhost:3979/callback` に値を設定します。

    ![[アプリケーションを登録する] ページのスクリーンショット](readme-images/aad-register-an-app.PNG)

1. [**登録**] を選択します。[**Approval Bot**] ページで、[**アプリケーション (クライアント) ID**] の値をコピーして保存します。この値は次の手順で必要になります。

    ![新しいアプリ登録のアプリケーション ID のスクリーンショット](readme-images/aad-application-id.PNG)

1. [**管理**] で [**証明書とシークレット**] を選択します。[**新しいクライアント シークレット**] ボタンを選択します。[**説明**] に値を入力し、[**有効期限**] のオプションのいずれかを選び、[**追加**] を選択します。

    ![[クライアント シークレットの追加] ダイアログのスクリーンショット](readme-images/aad-new-client-secret.png)

1. このページを離れる前に、クライアント シークレットの値をコピーします。この値は次の手順で必要になります。

    > [重要!]
    > このクライアント シークレットは今後表示されないため、この段階で必ずコピーするようにしてください。

    ![新規追加されたクライアント シークレットのスクリーンショット](readme-images/aad-copy-client-secret.png)

### ngrok プロキシをセットアップする

Bot Framework Emulator から通知を受信するには、パブリック HTTPS エンドポイントを公開する必要があります。テスト中は、ngrok を使用して Bot Framework Emulator からのメッセージをコンピューター上の *localhost* ポートにトンネルすることを一時的に許可できます。

ngrok Web インターフェイス ([http://127.0.0.1:4040](http://127.0.0.1:4040)) を使用して、トンネルを通過する HTTP トラフィックを検査できます。ngrok の使用方法の詳細については、「[ngrok の Web サイト](https://ngrok.com/)」を参照してください。


1. Windows 用の [ngrok をダウンロード](https://ngrok.com/download)します。

1. パッケージを解凍し、ngrok.exe を実行します。

1. ngrok コンソールで、次のコマンド ラインを実行します。

    ```Shell
    ngrok http 3979 --host-header=localhost:3979
    ```

    ![ngrok コンソールで実行するコマンドの例](readme-images/ngrok1.PNG)

1. コンソールに表示される HTTPS URL をコピーします。これを使用して、サンプル内で`NgrokRootUrl`を構成します。

    ![ngrok コンソールに表示される転送用 HTTPS URL](readme-images/ngrok2.PNG)

    > **注:**テスト中はコンソールを開いたままにします。コンソールを閉じるとトンネルも閉じられるため、新しい URL を生成してサンプルを更新する必要があります。

### サンプルを構成して実行する

1. ローカルでリポジトリを複製します。
1. 同じディレクトリにある [**./ApprovalBot/PrivateSettings.example.config**] ファイルのコピーを作成し、`PrivateSettings.config` と名前を付けます。
1. Visual Studio で、[**ApprovalBot.sln**] を開いて、[**PrivateSettings.config**] ファイルを開きます。 

1. `MicrosoftAppId` の値を前の手順で生成したアプリケーション (クライアント) ID に設定し、`MicrosoftAppPassword` の値をその後で生成したシークレットに設定します。

1. 前の手順からコピーされた ngrok HTTPS URL の値を **PrivateSettings.config** の `NgrokRootUrl` の値に貼り付け、変更を保存します。

    > **重要**:サンプルの実行中は、ngrok を実行したままにします。ngrok を停止して再起動すると、転送の URL が変更され、`NgrokRootUrl` の値を更新する必要があります。

1. Azure Cosmos DB Emulator を開始します。これは、サンプルを開始する前に実行している必要があります。

1. F5 キーを押してサンプルをデバッグします。

1. Bot Framework Emulator を実行します。一番上の **エンドポイント URL を入力する** で、`https://localhost:3979/api/messages` を入力します。

1. アプリ ID とパスワードの入力を求められます。アプリ ID とシークレットを入力し、**ロケール** を空白にします。

    ![](readme-images/configure-emulator.PNG)

1. [**接続**] をクリックします。

1. 接続を確認するために、`hi` と送信します。

    ![](readme-images/hello-bot.PNG)

## ローカルで実行する場合の制限事項

ローカルでサンプルを実行する場合、送信される承認要求のメールが異なります。これは、確認済みの登録済みドメインで実行されていないためで、メッセージの送信には同じアカウントを使う必要があります。つまり、承認を要求するポイントに到達したら、承認者のリストに自分自身を含める必要があります。

他の承認者を含めることはできますが、それらの承認者が受信するメッセージにはアダプティブ カードが表示されません。Outlook で自分のメールボックスにログインして、アダプティブ カードをテストしてください。

## 投稿

このプロジェクトは投稿や提案を歓迎します。たいていの投稿には、投稿者のライセンス契約 (CLA)
に同意することにより、投稿内容を使用する権利を Microsoft に付与する権利が自分にあり、
実際に付与する旨を宣言していただく必要があります。詳細については、https://cla.microsoft.com をご覧ください。

プル要求を送信すると、CLA を提供する必要があるかどうかを CLA ボットが自動的に判断してプル要求を適切に修飾 (ラベル、コメントなど) します。
ボットの指示に従ってください。この操作は、
CLA を使用してすべてのリポジトリ全体に対して 1 度のみ行う必要があります。

このプロジェクトでは、[Microsoft Open Source Code of Conduct (Microsoft オープン ソース倫理規定)](https://opensource.microsoft.com/codeofconduct/)
が採用されています。詳細については、「[倫理規定の FAQ](https://opensource.microsoft.com/codeofconduct/faq/)」を参照してください。また、その他の質問やコメントがあれば、
[opencode@microsoft.com](mailto:opencode@microsoft.com) までお問い合わせください。

## 著作権

Copyright (c) 2019 Microsoft.All rights reserved.
