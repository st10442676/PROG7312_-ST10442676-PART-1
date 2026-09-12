< !DOCTYPE html >
< html lang = "en" >
< head >
    < meta charset = "utf-8" />

    < meta
        name = "viewport"
        content = "width=device-width, initial-scale=1.0" />

    < meta
        name = "theme-color"
        content = "#07110f" />

    < meta
        name = "description"
        content = "Smart-X is a secure IoT sensor management and telemetry platform." />

    < title > Smart - X | Intelligent IoT Gateway</title>

    <base href= "/" />

    < link
        rel= "preconnect"
        href= "https://fonts.googleapis.com" />

    < link
        rel= "preconnect"
        href= "https://fonts.gstatic.com"
        crossorigin />

    <link
        href = "https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&amp;family=Space+Grotesk:wght@500;600;700&amp;display=swap"
        rel= "stylesheet" />

    < link
        rel= "icon"
        type= "image/png"
        href= "favicon.png" />

    < link
        href= "css/app.css"
        rel= "stylesheet" />

    < link
        href= "SmartX.Client.styles.css"
        rel= "stylesheet" />
</ head >

< body >
    < div id= "app" >
        < div class= "startup-loader" role = "status" >
            < div class= "startup-loader__mark" aria - hidden = "true" >
                < span ></ span >
                < span ></ span >
                < span ></ span >
            </ div >

            < div >
                < strong > SMART - X </ strong >
                < p > Synchronising the gateway...</ p >
            </ div >
        </ div >
    </ div >

    < script src = "_framework/blazor.webassembly.js" ></ script >
</ body >
</ html >