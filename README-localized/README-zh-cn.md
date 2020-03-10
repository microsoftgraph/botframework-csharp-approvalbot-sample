---
page_type: sample
products:
- office-outlook
- office-onedrive
- ms-graph
languages:
- csharp
description: "示例机器人，使用自适应卡和 .NET Graph SDK 发送可操作邮件，请求审批，以在 OneDrive 上发布文件。"
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
# 审批机器人示例

## 本地运行

按照下列步骤启用本地运行机器人，以进行调试。

### 先决条件

- [ngrok](https://ngrok.com/)
- [机器人框架模拟器](https://github.com/Microsoft/BotFramework-Emulator/releases)
- [Azure Cosmos DB 模拟器](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- Visual Studio 2017

### 注册应用

1. 打开浏览器，并转到 [Azure Active Directory 管理中心](https://aad.portal.azure.com)。使用**工作或学校帐户**登录。

1. 选择左侧导航栏中的**“Azure Active Directory”**，再选择**“管理”**下的**“应用注册(预览版)”**。

    ![“应用注册”的屏幕截图 ](readme-images/aad-portal-app-registrations.png)

1. 选择**“新注册”**。在“**注册应用**”页上，按如下方式设置值。

    - 设置首选**名称** ，如`Approval Bot`。
    - 将“**支持的帐户类型**”设置为“**任何组织目录中的帐户**”。
    - 在“**重定向 URI**”下，将第一个下拉列表设置为“`Web`”，并将值设置为 `http://localhost:3979/callback`。

    ![“注册应用程序”页的屏幕截图](readme-images/aad-register-an-app.PNG)

1. 选择“**注册**”。在“**Approval Bot**” 页面上，复制并保存“**应用(客户端) ID**”的值，将在下一步中用到它。

    ![新应用注册的应用程序 ID 的屏幕截图](readme-images/aad-application-id.PNG)

1. 选择“**管理**”下的“**证书和密码**”。选择**新客户端密码**按钮。在**说明**中输入值，并选择一个**过期**选项，再选择**添加**。

    ![“添加客户端密码”对话框的屏幕截图](readme-images/aad-new-client-secret.png)

1. 离开此页前，先复制客户端密码值。将在下一步中用到它。

    > [重要提示！]
    > 此客户端密码不会再次显示，所以请务必现在就复制它。

    ![新添加的客户端密码的屏幕截图](readme-images/aad-copy-client-secret.png)

### 设置 ngrok 代理

必须公开公共 HTTPS 终结点才能从机器人框架模拟器接收通知。测试时，你可以使用 ngrok 临时允许消息从“机器人框架模拟器”经隧道传输至计算机上的 *localhost* 端口。

你可以使用 ngrok Web 界面 ([http://127.0.0.1:4040](http://127.0.0.1:4040)) 检查流经隧道的 HTTP 流量。若要了解与使用 ngrok 相关的详细信息，请参阅 [ngrok 网站](https://ngrok.com/)。


1. [下载 Windows 版 ngrok](https://ngrok.com/download)。

1. 解压包并运行 ngrok.exe。

1. 在 ngrok 控制台上，运行以下命令：

    ```Shell
    ngrok http 3979 --host-header=localhost:3979
    ```

    ![在 ngrok 控制台中运行的示例命令](readme-images/ngrok1.PNG)

1. 复制控制台中显示的 HTTPS URL。你将使用它来配置示例中的 `NgrokRootUrl`。

    ![ngrok 控制台中的转发 HTTPS URL](readme-images/ngrok2.PNG)

    > **注意：**测试时，请保持控制台处于打开状态。如果关闭，则隧道也会关闭，并且你需要生成新的 URL 并更新示例。

### 配置并运行示例

1. 本地克隆存储库。
1. 在同一目录中创建文件 **./ApprovalBot/PrivateSettings.example.config** 的副本，并命名副本为 `PrivateSettings.config`。
1. 在 Visual Studio 中打开 **ApprovalBot.sln**，随后打开 **PrivateSettings.config** 文件。

1. 将 `MicrosoftAppId` 值设置为在上一步中生成的应用程序（客户端） ID，并将 `MicrosoftAppPassword` 值设置为之后生成的密码。

1. 将复制自上一步的 ngrok HTTPS URL 值粘贴至 **PrivateSettings.config** 的 `NgrokRootUrl` 值中，并保存更改。

    > **重要提示**：运行示例时，保持 ngrok 运行。如果停止 ngrok 并重启，转发 URL 发生更改，则需要更新 `NgrokRootUrl` 值。

1. 启动 Azure Cosmos DB 模拟器。启动示例前，需要运行此模拟器。

1. 按下 F5 以调试示例。

1. 运行“机器人框架模拟器”。在显示“**输入终结点 URL**”的顶部，输入 `https://localhost:3979/api/messages`。

1. 此操作将提示应用 ID 和密码。输入应用 ID 和密码，并将**区域设置**留空。

    ![](readme-images/configure-emulator.PNG)

1. 单击“**连接**”。

1. 发送 `hi` 以确认连接。

    ![](readme-images/hello-bot.PNG)

## 本地运行时的限制

本地运行示例时，已发送的审批请求稍有不同。因为未在经过确认的注册域上运行，所以发送和接收的账户是同一账户。即到达请求审批的位时，必须将自身包含在审批列表中。

可包含其他审批，但是收到的邮件不显示自适应卡。使用 Outlook 登录到邮箱，以测试自适应卡。

## 参与

本项目欢迎供稿和建议。
大多数的供稿都要求你同意“参与者许可协议 (CLA)”，声明你有权并确定授予我们使用你所供内容的权利。
有关详细信息，请访问 https://cla.microsoft.com。

在提交拉取请求时，CLA 机器人会自动确定你是否需要提供 CLA 并适当地修饰
PR（例如标记、批注）。只需按照机器人提供的说明操作即可。
只需在所有存储库上使用我们的 CLA 执行此操作一次。

此项目已采用 [Microsoft 开放源代码行为准则](https://opensource.microsoft.com/codeofconduct/)。
有关详细信息，请参阅[行为准则常见问题解答](https://opensource.microsoft.com/codeofconduct/faq/)。
如有其他任何问题或意见，也可联系 [opencode@microsoft.com](mailto:opencode@microsoft.com)。

## 版权信息

版权所有 (c) 2019 Microsoft。保留所有权利。
