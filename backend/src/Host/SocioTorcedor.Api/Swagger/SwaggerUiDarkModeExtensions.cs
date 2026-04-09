using Swashbuckle.AspNetCore.SwaggerUI;

namespace SocioTorcedor.Api.Swagger;

/// <summary>
/// Tema escuro do Swagger UI 5.x: classe <c>dark-mode</c> em <c>document.documentElement</c>,
/// botão flutuante para alternar e preferência em <c>localStorage</c>.
/// </summary>
public static class SwaggerUiDarkModeExtensions
{
    private const string StorageKey = "socioTorcedor.swagger.darkMode";

    /// <summary>Injeta script no <c>HeadContent</c> do Swagger UI (Swashbuckle).</summary>
    public static void UseSocioTorcedorSwaggerDarkMode(this SwaggerUIOptions options)
    {
        // JavaScript minificado em uma linha evita ambiguidade com literais raw/interpolados.
        var script =
            "<script>" +
            "(function(){var K='" + StorageKey + "';" +
            "function sd(){return window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches;}" +
            "function rd(){var v=localStorage.getItem(K);if(v==='1')return true;if(v==='0')return false;return sd();}" +
            "function ap(d){document.documentElement.classList.toggle('dark-mode',d);}ap(rd());" +
            "function lb(){return document.documentElement.classList.contains('dark-mode')?'Claro':'Escuro';}" +
            "function go(){" +
            "if(document.getElementById('socio-torcedor-swagger-theme-btn'))return;" +
            "var b=document.createElement('button');b.id='socio-torcedor-swagger-theme-btn';b.type='button';" +
            "b.title='Alternar tema claro/escuro (Swagger)';b.setAttribute('aria-label',b.title);b.textContent=lb();" +
            "b.style.cssText='position:fixed;bottom:14px;right:14px;z-index:10000;padding:8px 12px;border-radius:8px;font:13px system-ui,sans-serif;cursor:pointer;box-shadow:0 1px 4px rgba(0,0,0,.2)';" +
            "b.addEventListener('click',function(){" +
            "var d=!document.documentElement.classList.contains('dark-mode');ap(d);localStorage.setItem(K,d?'1':'0');b.textContent=lb();});" +
            "window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change',function(){" +
            "if(localStorage.getItem(K)===null)ap(sd());});" +
            "(document.body||document.documentElement).appendChild(b);}" +
            "if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',go);else go();})();" +
            "</script>";

        options.HeadContent = (options.HeadContent ?? string.Empty) + script;
    }
}
