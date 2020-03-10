---
page_type: sample
products:
- office-outlook
- office-onedrive
- ms-graph
languages:
- csharp
description: "Exemplo de Bot que usa cartões adaptativo e o SDK do Graph para .NET a fim de enviar mensagens acionáveis solicitando a aprovação de liberação de arquivos no OneDrive."
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
# Exemplo de Bot de Aprovação

## Executado localmente

Siga estas etapas para habilitar a execução local do bot para depuração.

### Pré-requisitos

- [ngrok](https://ngrok.com/)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
- [Emulador do Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- Visual Studio 2017

### Registrar o aplicativo

1. Abra um navegador e navegue até o [centro de administração do Azure Active Directory](https://aad.portal.azure.com). Faça o login usando uma **Conta Corporativa ou de Estudante**.

1. Selecione **Azure Active Directory** na navegação à esquerda e, em seguida, selecione **Registros de aplicativo (Visualizar)** em **Gerenciar**.

    ![Captura de tela dos Registros de aplicativo](readme-images/aad-portal-app-registrations.png)

1. Selecione **Novo registro**. Na página **Registrar um aplicativo**, defina os valores da seguinte forma.

    - Defina um **Nome** adequado, por exemplo, `Bot de Aprovação`.
    - Defina os **Tipos de conta com suporte** como **Contas em qualquer diretório organizacional**.
    - Em **URI de Redirecionamento**, defina o primeiro menu suspenso como `Web` e defina o valor como `http://localhost:3979/callback`.

    ![Captura de tela da página Registrar um aplicativo](readme-images/aad-register-an-app.PNG)

1. Escolha **Registrar**. Na página **Bot de Aprovação**, copie e salve o valor da **ID do aplicativo (cliente)**, você precisará dele na próxima etapa.

    ![Captura de tela da ID do aplicativo do novo registro do aplicativo](readme-images/aad-application-id.PNG)

1. Selecione **Certificados e segredos** em **Gerenciar**. Selecione o botão **Novo segredo do cliente**. Insira um valor em **Descrição**, selecione uma das opções para **Expira** e escolha **Adicionar**.

    ![Uma captura de tela da caixa de diálogo Adicionar um segredo do cliente](readme-images/aad-new-client-secret.png)

1. Copie o valor secreto do cliente antes de sair desta página. Você precisará dele na próxima etapa.

    > [!IMPORTANTE]
    > Este segredo do cliente nunca é mostrado novamente, portanto, copie-o agora.

    ![Captura de tela do segredo do cliente recém-adicionado](readme-images/aad-copy-client-secret.png)

### Configurar o proxy ngrok

Você deve expor um ponto de extremidade HTTPS público para receber notificações do Bot Framework Emulator. Ao testar, você pode usar o ngrok para permitir temporariamente que as mensagens do Bot Framework Emulator sejam encapsuladas para uma porta do *localhost* em seu computador.

Você pode usar a interface da Web do ngrok ([http://127.0.0.1:4040](http://127.0.0.1:4040)) para inspecionar o tráfego HTTP que passa pelo encapsulamento. Para saber mais sobre como usar o ngrok, confira o [site do ngrok](https://ngrok.com/).


1. [Baixar ngrok](https://ngrok.com/download) para Windows.

1. Descompacte o pacote e execute ngrok.exe.

1. Execute o seguinte comando no console do ngrok:

    ```Shell
    ngrok http 3979 --host-header=localhost:3979
    ```

    ![Exemplo do comando a executar no console ngrok](readme-images/ngrok1.PNG)

1. Copie a URL HTTPS exibida no console. Você usará essa configuração para configurar a `NgrokRootUrl` no exemplo.

    ![A URL HTTPS de encaminhamento no console ngrok](readme-images/ngrok2.PNG)

    > **Observação:** Mantenha o console aberto durante o teste. Caso você o feche, o encapsulamento também será fechado, e você precisará gerar uma nova URL e atualizar o exemplo.

### Configurar e executar o exemplo

1. Clonar o repositório localmente.
1. Faça uma cópia do arquivo **./ApprovalBot/PrivateSettings.example.config** no mesmo diretório e nomeie a cópia como `PrivateSettings.config`.
1. Abra **ApprovalBot.sln** no Visual Studio e abra o arquivo **PrivateSettings.config**.

1. Configure o valor de `MicrosoftAppId` com a ID do aplicativo (cliente) gerada na etapa anterior e configure o valor de `MicrosoftAppPassword` com o segredo gerado posteriormente.

1. Cole o valor da URL HTTPS do ngrok copiado na etapa anterior no valor de `NgrokRootUrl` em **PrivateSettings.config**e salve suas alterações.

    > **IMPORTANTE**: deixe ngrok funcionando enquanto você executa o exemplo. Se você interromper e reiniciar o ngrok, a URL de encaminhamento será alterada, e você precisará atualizar o valor de `NgrokRootUrl`.

1. Inicie o Emulador do Azure Cosmos DB. Ele deve estar em execução antes de iniciar o exemplo.

1. Pressione F5 para depurar o exemplo.

1. Execute o Bot Framework Emulator. Na parte superior, onde diz **Insira a URL do ponto de extremidade**, digite `https://localhost:3979/api/messages`.

1. Isso solicita a ID do aplicativo e a senha. Insira a ID do aplicativo e o segredo e deixe a **Localidade** em branco.

    ![](readme-images/configure-emulator.PNG)

1. Clique em **Conectar**.

1. Envie `hi`para confirmar a conexão.

    ![](readme-images/hello-bot.PNG)

## Limitações durante a execução local

Ao executar o exemplo localmente, os emails da solicitação de aprovação enviados são um pouco diferentes. Como não está sendo executado em um domínio registrado e confirmado, devemos enviar a mensagem de e para a mesma conta. Isso significa que, quando você chegar ao ponto em que solicitou a aprovação, você deve incluir a si mesmo na lista de aprovadores.

Você pode incluir outros aprovadores, mas a mensagem que receber não mostrará o cartão adaptável. Entre em sua caixa de correio com o Outlook para testar o cartão adaptável.

## Colaboração

Este projeto recebe e agradece as contribuições e sugestões.
A maioria das contribuições exige que você concorde com um Contrato de Licença de Colaborador (CLA) declarando que você tem o direito a,
nos conceder os direitos de uso de sua contribuição, e de fato o faz. Para saber mais, acesse https://cla.microsoft.com.

Quando você envia uma solicitação de pull, um bot de CLA determina automaticamente se você precisa fornecer um CLA e decora o PR adequadamente (por exemplo, rótulo, comentário).
Basta seguir as instruções fornecidas pelo bot.
Você só precisa fazer isso uma vez em todos os repos que usam nosso CLA.

Este projeto adotou o [Código de Conduta de Código Aberto da Microsoft](https://opensource.microsoft.com/codeofconduct/).
Para saber mais, confira as [Perguntas frequentes sobre o Código de Conduta](https://opensource.microsoft.com/codeofconduct/faq/)
ou entre em contato pelo [opencode@microsoft.com](mailto:opencode@microsoft.com) se tiver outras dúvidas ou comentários.

## Direitos autorais

Copyright (c) 2019 Microsoft. Todos os direitos reservados.
