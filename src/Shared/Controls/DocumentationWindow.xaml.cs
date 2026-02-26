using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LectorHuellas.Shared.Controls
{
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow(bool isAdminMode, int roleId = 0)
        {
            InitializeComponent();
            LoadContent(isAdminMode, roleId);
        }

        private void LoadContent(bool isAdminMode, int roleId)
        {
            DocViewer.Blocks.Clear();
            
            if (isAdminMode)
            {
                ContextTitle.Text = "MODO ADMINISTRADOR";
                AddAdminDocs(roleId);
            }
            else
            {
                ContextTitle.Text = "MODO ASISTENCIA";
                AddAttendanceDocs();
            }
        }

        private void AddAttendanceDocs()
        {
            AddSection("✋ Cómo Registrar Asistencia", "Siga estos pasos para marcar su entrada o salida del sistema.");
            
            AddStep("1. Ubique el sensor", "Asegúrese de que el lector de huellas esté encendido (luz azul constante).");
            AddStep("2. Coloque su dedo", "Posicione el dedo registrado (generalmente el índice derecho) de forma plana sobre el cristal.");
            AddStep("3. Presione suavemente", "Mantenga la presión por 1-2 segundos hasta que escuche el pitido o vea su foto en pantalla.");
            
            AddSection("💡 Consejos Útiles", "");
            AddBullet("Limpieza: Si el sensor está sucio, límpielo con un paño seco.");
            AddBullet("Humedad: Si tiene los dedos muy secos, frótelos un poco para generar calor natural.");
            AddBullet("Error: Si el sistema no lo reconoce, intente un dedo alternativo registrado.");
        }

        private void AddAdminDocs(int roleId)
        {
            AddSection("👥 Gestión de Empleados", "Como administrador, puede gestionar el personal y sus datos biométricos.");
            
            AddHeader("Registro de Nuevo Personal");
            AddBullet("Vaya a la pestaña 'Empleados' y pulse '+ Nuevo Empleado'.");
            AddBullet("Complete los datos básicos (Cédula, Nombres, Departamento).");
            
            AddHeader("Editar o Eliminar Personal");
            AddBullet("En la lista de empleados, localice la columna 'Acciones' a la derecha.");
            AddBullet("Pulse el botón del lápiz (✏️) para editar los datos de un empleado.");
            AddBullet("Pulse el botón de la papelera (🗑️) para eliminar a un empleado.");

            AddHeader("Captura Biométrica");
            AddBullet("En el formulario de edición o creación, pulse 'Capturar Huella'.");
            AddBullet("Pida al empleado que coloque el dedo 3 veces seguidas para una lectura óptima.");

            if (roleId != 3)
            {
                AddSection("⚙️ Configuración del Sistema", "Ajustes avanzados para el funcionamiento técnico.");
                AddBullet("Base de Datos: Puede cambiar el host y credenciales en el panel de Configuración.");
                AddBullet("Modo Oscuro: Personalice la interfaz según la iluminación del entorno.");
            }
        }

        private void AddSection(string title, string intro)
        {
            var header = new Paragraph(new Run(title)) { FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 10) };
            DocViewer.Blocks.Add(header);
            
            if (!string.IsNullOrEmpty(intro))
            {
                var p = new Paragraph(new Run(intro)) { Margin = new Thickness(0, 0, 0, 15), Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush") };
                DocViewer.Blocks.Add(p);
            }
        }

        private void AddHeader(string text)
        {
            var p = new Paragraph(new Run(text)) { FontWeight = FontWeights.SemiBold, FontSize = 16, Margin = new Thickness(0, 15, 0, 5) };
            DocViewer.Blocks.Add(p);
        }

        private void AddStep(string title, string description)
        {
            var p = new Paragraph();
            p.Inlines.Add(new Run(title) { FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.FindResource("PrimaryLightBrush") });
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run(description));
            p.Margin = new Thickness(10, 5, 0, 10);
            DocViewer.Blocks.Add(p);
        }

        private void AddBullet(string text)
        {
            var list = new List();
            list.ListItems.Add(new ListItem(new Paragraph(new Run("• " + text))));
            list.Margin = new Thickness(15, 2, 0, 2);
            DocViewer.Blocks.Add(list);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
