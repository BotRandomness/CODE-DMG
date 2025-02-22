/*
*   File Dialog for ImGui.NET
*   This is a simple file dialog, it's flexible and expandable
*   This can also easily be ported to other languages for other bindings or the original Dear ImGui
*
*   Basic Usage:
*   FileDialog fileDialog = new FileDialog();
*
*   string selectedFilePath = "";
*
*   fileDialog.Open(); //Trigger the file dialog to open
*
*   if (fileDialog.Show(ref selectedFilePath)) {
*       //Do something with string path
*   }
*   
*   Made by Bot Randomness
*/

using ImGuiNET;

class FileDialog {
    private string currentDirectory;
    private string selectedFilePath = "";
    private bool isOpen = false;

    private bool canCancel = true;

    public FileDialog(string startDirectory, bool canCancel = true) {
        currentDirectory = Directory.Exists(startDirectory) ? startDirectory : Directory.GetCurrentDirectory();
        this.canCancel = canCancel;
    }

    public bool Show(ref string resultFilePath) {
        if (!isOpen) return false;

        bool fileSelected = false;

        if (ImGui.Begin("File Dialog", ImGuiWindowFlags.HorizontalScrollbar)) {
            ImGui.Text("Select a file:");

            string[] directories = Directory.GetDirectories(currentDirectory);
            string[] files = Directory.GetFiles(currentDirectory, "*.*");

            ImGui.InputText("File Path", ref selectedFilePath, 260);

            if (ImGui.Button("Select File")) {
                if (File.Exists(selectedFilePath)) {
                    resultFilePath = selectedFilePath;
                    fileSelected = true;
                    isOpen = false;
                }
            }

            ImGui.SameLine();

            if (canCancel) {
                if (ImGui.Button("Cancel")) {
                    isOpen = false;
                }
            }

            if (Path.GetPathRoot(currentDirectory) != currentDirectory) {
                if (ImGui.Button("Back")) {
                    currentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
                }
            }

            foreach (var dir in directories) {
                if (ImGui.Selectable("[DIR] " + Path.GetFileName(dir))) {
                    currentDirectory = dir;
                }
            }

            foreach (var file in files) {
                if (ImGui.Selectable(Path.GetFileName(file))) {
                    selectedFilePath = file;
                }
            }

            if (directories.Length == 0 && files.Length == 0) {
                ImGui.Text("No files or folders found.");
            }
        }
        ImGui.End();

        return fileSelected;
    }

    public void Open() {
        isOpen = true;
    }
}
