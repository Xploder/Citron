using System;
using System.Collections.Generic;
using System.Globalization;

namespace Citron;
internal static class Strings
{
    internal static string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    // EN, DE, ES, FR, PT. Keep all user-facing actions in this catalog.
    static readonly Dictionary<string, string[]> Catalog = new()
    {
        ["subtitle"] = ["A little energy for your workspace.", "Neue Energie für deinen Workspace.", "Un poco de energía para tu espacio de trabajo.", "Un peu d’énergie pour votre espace de travail.", "Um pouco de energia para o seu espaço de trabalho."],
        ["target"] = ["Citrix session", "Citrix-Sitzung", "Sesión de Citrix", "Session Citrix", "Sessão Citrix"],
        ["refresh"] = ["Find sessions", "Sitzungen suchen", "Buscar sesiones", "Rechercher les sessions", "Buscar sessões"],
        ["enable"] = ["Keep session active", "Sitzung aktiv halten", "Mantener la sesión activa", "Maintenir la session active", "Manter a sessão ativa"],
        ["interval"] = ["Interval (seconds)", "Intervall (Sekunden)", "Intervalo (segundos)", "Intervalle (secondes)", "Intervalo (segundos)"],
        ["duration"] = ["Duration (minutes, 0 = unlimited)", "Dauer (Minuten, 0 = unbegrenzt)", "Duración (minutos, 0 = ilimitada)", "Durée (minutes, 0 = illimitée)", "Duração (minutos, 0 = ilimitada)"],
        ["language"] = ["Language", "Sprache", "Idioma", "Langue", "Idioma"],
        ["sleep"] = ["Also keep this PC awake", "Diesen PC ebenfalls wach halten", "Mantener también este PC activo", "Garder aussi ce PC éveillé", "Manter também este PC ativo"],
        ["setup"] = ["Set up Citrix access", "Citrix-Zugriff einrichten", "Configurar acceso a Citrix", "Configurer l’accès Citrix", "Configurar acesso ao Citrix"],
        ["restore"] = ["Restore previous settings", "Vorherige Einstellungen wiederherstellen", "Restaurar ajustes anteriores", "Restaurer les paramètres précédents", "Restaurar definições anteriores"],
        ["cancel"] = ["Cancel", "Abbrechen", "Cancelar", "Annuler", "Cancelar"],
        ["continue"] = ["Continue", "Weiter", "Continuar", "Continuer", "Continuar"],
        ["close"] = ["Close", "Schließen", "Cerrar", "Fermer", "Fechar"],
        ["settings"] = ["Open Citron", "Citron öffnen", "Abrir Citron", "Ouvrir Citron", "Abrir Citron"],
        ["toggle"] = ["Enable / pause", "Aktivieren / pausieren", "Activar / pausar", "Activer / suspendre", "Ativar / pausar"],
        ["exit"] = ["Quit", "Beenden", "Salir", "Quitter", "Sair"],
        ["idle"] = ["Paused", "Pausiert", "En pausa", "En pause", "Em pausa"],
        ["active"] = ["Active · delivery is not independently verified", "Aktiv · Zustellung nicht unabhängig bestätigt", "Activo · entrega sin verificación independiente", "Actif · réception non vérifiée indépendamment", "Ativo · entrega não verificada independentemente"],
        ["none"] = ["No session found. Connect Citrix, then refresh.", "Keine Sitzung gefunden. Citrix verbinden und erneut suchen.", "No se encontró ninguna sesión. Conecta Citrix y busca de nuevo.", "Aucune session trouvée. Connectez Citrix et relancez la recherche.", "Nenhuma sessão encontrada. Conecte o Citrix e procure novamente."],
        ["error"] = ["Citrix access failed", "Citrix-Zugriff fehlgeschlagen", "Error de acceso a Citrix", "Échec de l’accès Citrix", "Falha no acesso ao Citrix"],
        ["setupText"] = ["Citron will enable the following DWORD values (1) for the detected Citrix component. Windows will request administrator approval. Previous values are saved for restoration.", "Citron aktiviert die folgenden DWORD-Werte (1) für die erkannte Citrix-Komponente. Windows fragt nach Administratorrechten. Vorherige Werte werden zur Wiederherstellung gesichert.", "Citron activará los siguientes valores DWORD (1). Windows solicitará permisos de administrador. Se guardarán los valores anteriores.", "Citron activera les valeurs DWORD suivantes (1). Windows demandera les droits administrateur. Les anciennes valeurs seront sauvegardées.", "O Citron ativará os seguintes valores DWORD (1). O Windows solicitará direitos de administrador. Os valores anteriores serão guardados."],
        ["done"] = ["Settings updated. Refresh sessions; Workspace may need a manual restart.", "Einstellungen aktualisiert. Sitzungen erneut suchen; Workspace benötigt eventuell einen manuellen Neustart.", "Ajustes actualizados. Busca sesiones; puede ser necesario reiniciar Workspace manualmente.", "Paramètres actualisés. Relancez la recherche ; un redémarrage manuel de Workspace peut être nécessaire.", "Definições atualizadas. Procure sessões; poderá ser necessário reiniciar o Workspace manualmente."],
        ["locked"] = ["Paused while Windows is locked", "Bei gesperrtem Windows pausiert", "En pausa mientras Windows está bloqueado", "En pause lorsque Windows est verrouillé", "Em pausa enquanto o Windows está bloqueado"],
        ["setupNeeded"] = ["Citrix access needs setup", "Citrix-Zugriff muss eingerichtet werden", "Es necesario configurar el acceso", "L’accès Citrix doit être configuré", "É necessário configurar o acesso"],
        ["about"] = ["Experimental Citrix compatibility • Double-click the tray icon to toggle • Closing this window keeps Citron in the tray.", "Experimentelle Citrix-Kompatibilität • Tray-Doppelklick zum Umschalten • Beim Schließen läuft Citron im Tray weiter.", "Compatibilidad experimental • Doble clic en el icono para activar • Al cerrar, Citron permanece en la bandeja.", "Compatibilité expérimentale • Double-clic sur l’icône pour activer • Fermer conserve Citron dans la zone de notification.", "Compatibilidade experimental • Duplo clique no ícone para ativar • Ao fechar, o Citron permanece na área de notificação."],
    };
    internal static string T(string key) => Catalog[key][Language switch { "de" => 1, "es" => 2, "fr" => 3, "pt" => 4, _ => 0 }];
}
