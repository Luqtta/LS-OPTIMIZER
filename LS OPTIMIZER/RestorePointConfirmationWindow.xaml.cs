using System.Windows;

namespace LS_OPTIMIZER
{
    public partial class RestorePointConfirmationWindow : Window
    {
        public bool RememberChoice { get; private set; } = false;
        public bool UserChoice { get; private set; } = false;

        public RestorePointConfirmationWindow()
        {
            InitializeComponent();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = true;
            RememberChoice = RememberChoiceCheckBox.IsChecked == true;
            this.DialogResult = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = false;
            RememberChoice = RememberChoiceCheckBox.IsChecked == true;
            this.DialogResult = true;
            this.Close();
        }
    }
}
