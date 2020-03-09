---
page_type: sample
products:
- office-outlook
- office-onedrive
- ms-graph
languages:
- csharp
description: "Un bot de ejemplo que usa tarjetas adaptables y el SDK de .NET Graph para enviar mensajes que requieren acción y que solicitan aprobación para publicar archivos en OneDrive."
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
# Ejemplo de bot de aprobación

## Ejecución local

Siga los pasos que se indican a continuación para habilitar la ejecución del bot de forma local para la depuración.

### Requisitos previos

- [ngrok](https://ngrok.com/)
- [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/releases)
- [Azure Cosmos DB Emulator](https://docs.microsoft.com/es-es/azure/cosmos-db/local-emulator)
- Visual Studio 2017

### Registrar la aplicación

1. Abra el explorador y vaya al [Centro de administración de Azure Active Directory](https://aad.portal.azure.com). Inicie sesión con **una cuenta profesional o educativa**.

1. Seleccione **Azure Active Directory** en el panel de navegación izquierdo y, después, seleccione **Registros de aplicaciones (versión preliminar)** en **Administrar**.

    ![Una captura de pantalla de los registros de la aplicación ](readme-images/aad-portal-app-registrations.png)

1. Seleccione **Nuevo registro**. En la página **Registrar una aplicación**, establezca los valores siguientes.

    - Establezca un **Nombre** de preferencia; por ejemplo, `Bot de aprobación`.
    - Establezca los **Tipos de cuenta compatibles** en **Cuentas de cualquier directorio organizativo**.
    - En **URI de redirección**, establezca la primera lista desplegable en `Web` y el valor en `http://localhost:3979/callback`.

    ![Captura de pantalla de la página Registrar una aplicación](readme-images/aad-register-an-app.PNG)

1. Haga clic en **Registrar**. En la página de **Bot de aprobación**, copie el valor de **Id. de aplicación (cliente)** y guárdelo, lo necesitará en el paso siguiente.

    ![Captura de pantalla del Id. de aplicación del nuevo registro](readme-images/aad-application-id.PNG)

1. Seleccione **Certificados y secretos** en **Administrar**. Seleccione el botón **Nuevo secreto de cliente**. Escriba un valor en **Descripción** y seleccione una de las opciones de **Expirar** y luego seleccione **Agregar**.

    ![Captura de pantalla del diálogo Agregar un cliente secreto](readme-images/aad-new-client-secret.png)

1. Copie el valor del secreto del cliente antes de salir de esta página. Lo necesitará en el siguiente paso.

    > [¡IMPORTANTE!]
    > El secreto del cliente no volverá a ser mostrado, asegúrese de copiarlo en este momento.

    ![Captura de pantalla del nuevo secreto de cliente agregado](readme-images/aad-copy-client-secret.png)

### Configurar el proxy ngrok

Debe exponer un punto de conexión HTTPS público para recibir las notificaciones desde Bot Framework Emulator. Mientras realiza las pruebas, puede usar ngrok para permitir temporalmente que los mensajes de Bot Framework Emulator establezcan un túnel hacia un puerto de *localhost* en su computadora.

Puede usar la interfaz web de ngrok ([http://127.0.0.1:4040](http://127.0.0.1:4040)) para inspeccionar el tráfico HTTP que pasa por el túnel. Para obtener más información sobre el uso de ngrok, visite el [sitio web de ngrok](https://ngrok.com/).


1. [Descargue ngrok](https://ngrok.com/download) para Windows.

1. Descomprima el paquete y ejecute ngrok.exe.

1. En la consola de ngrok, ejecute el siguiente comando:

    ```Shell
    ngrok http 3979 --host-header=localhost:3979
    ```

    ![Comando de ejemplo para ejecutar en la consola de ngrok](readme-images/ngrok1.PNG)

1. Copie la dirección URL HTTPS que se muestra en la consola. Esto se usará para configurar la `NgrokRootUrl` en el ejemplo.

    ![La dirección URL HTTPS de reenvío en la consola de ngrok](readme-images/ngrok2.PNG)

    > **Nota:** Mantenga la consola abierta mientras realiza las pruebas. Si la cierra, el túnel también se cerrará y tendrá que generar una nueva dirección URL y actualizar el ejemplo.

### Configurar y ejecutar el ejemplo

1. Clone el repositorio de forma local.
1. Haga una copia del archivo **./ApprovalBot/PrivateSettings.example.config** en el mismo directorio y asigne a la copia el nombre `PrivateSettings.config`.
1. Abra **ApprovalBot.sln** en Visual Studio y luego abra el archivo **PrivateSettings.config**.

1. Configure el valor de `MicrosoftAppId` con el Id. de aplicación (cliente) generado en el paso anterior y configure el valor de `MicrosoftAppPassword` con el secreto que generado posteriormente.

1. Pegue el valor de la dirección URL HTTPS de ngrok copiado del paso anterior en el valor de `NgrokRootUrl` en **PrivateSettings.config** y guarde los cambios.

    > **IMPORTANTE**: Deje ngrok en ejecución mientras ejecuta el ejemplo. Si detiene ngrok y lo reinicia, la dirección URL de reenvío cambiará y tendrá que actualizar el valor de `NgrokRootUrl`.

1. Inicie Azure Cosmos DB Emulator. Debe estar en ejecución antes de iniciar el ejemplo.

1. Presione F5 para depurar el ejemplo.

1. Ejecute Bot Framework Emulator. En la parte superior, donde dice **Enter your endpoint URL**, escriba `https://localhost:3979/api/messages`.

1. Se le pedirá el Id. de aplicación y la contraseña. Escriba el Id. de aplicación y el secreto, y deje **Locale** en blanco.

    ![](readme-images/configure-emulator.PNG)

1. Haga clic en **Connect**.

1. Envíe `hola` para confirmar la conexión.

    ![](readme-images/hello-bot.PNG)

## Limitaciones al ejecutar de forma local

Cuando se ejecuta el ejemplo de forma local, el correo electrónico de solicitud de aprobación que se envía es un poco diferente. Debido a que no se ejecuta en un dominio registrado y confirmado, se envía el mensaje a y desde la misma cuenta. Esto significa que, cuando llegue al punto en el que solicita la aprobación, debe incluirse a usted mismo en la lista de aprobadores.

Puede incluir otros aprobadores, pero el mensaje que reciben no mostrará la tarjeta adaptable. Inicie sesión en su propio buzón con Outlook para probar la tarjeta adaptable.

## Contribuciones

Este proyecto recibe las contribuciones y las sugerencias.
La mayoría de las contribuciones requiere que acepte un Contrato de Licencia de Colaborador (CLA) donde declara que tiene el derecho, y realmente lo tiene, de otorgarnos los derechos para usar su contribución.
Para obtener más información, visite https://cla.microsoft.com.

Cuando envíe una solicitud de incorporación de cambios, un bot de CLA determinará automáticamente si necesita proporcionar un CLA y agregar el PR correctamente (por ejemplo, etiqueta, comentario).
Siga las instrucciones proporcionadas por el bot.
Solo debe hacerlo una vez en todos los repositorios que usen nuestro CLA.

Este proyecto ha adoptado el [Código de conducta de código abierto de Microsoft](https://opensource.microsoft.com/codeofconduct/).
Para obtener más información, vea [Preguntas frecuentes sobre el código de conducta](https://opensource.microsoft.com/codeofconduct/faq/)
o póngase en contacto con [opencode@microsoft.com](mailto:opencode@microsoft.com) si tiene otras preguntas o comentarios.

## Copyright

Copyright (c) 2019 Microsoft. Todos los derechos reservados.
