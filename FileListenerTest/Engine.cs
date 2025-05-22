using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace FileListenerTest
{
    class Engine
    {
        public static string BasePath = "";
        private static XmlDocument document = new XmlDocument();

        private static void LoadDocument() {
            document.Load(BasePath);
        }
        private static void SaveDocument() {
            document.Save(BasePath);
        }

        private static string PathToXml(string path, int type) {
            string xmlPath = "/root";

            if (path.Length > 0) {
                string[] nodes = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                for (int i = 0; i < nodes.Length - 1; i++) {
                    xmlPath += "/directory[@name='" + StringToHex(nodes[i]) + "']";
                }

                if (type == 0) {
                    xmlPath += "/directory[@name='" + StringToHex(nodes[nodes.Length - 1]) + "']";
                } else if (type == 1) {
                    xmlPath += "/file[@name='" + StringToHex(nodes[nodes.Length - 1]) + "']";
                }
            }

            return xmlPath;
        }
        private static string StringToHex(string text)
        {
            string newText = "";

            byte[] ba = Encoding.Default.GetBytes(text);
            newText = BitConverter.ToString(ba);

            return newText;
        }
        private static string HexToString(string text)
        {
            string newText = "";

            text = text.Replace("-", "");
            byte[] raw = new byte[text.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }

            newText = Encoding.ASCII.GetString(raw);

            return newText;
        }

        private static bool IfValueMatchesInArray(string[] array, string value) {
            bool ifSame = false;

            for (int i = 0; i < array.Length; i++) {
                if (array[i].Equals(value)) {
                    ifSame = true;

                    goto after_loop;
                }
            }
            after_loop:

            return ifSame;
        }
        private static string GetDirectoryName(string parentPath, string name, int type) {
            string newName = name;

            string[] directories = GetAllDirectories(parentPath);

            for (int i = 0; i < directories.Length; i++) {
                directories[i] = Path.GetFileName(directories[i]);
            }

            bool cont = true;
            int loops = 1;
            while (cont) {
                string valueCompare = (type == 0 ? 
                                        (loops == 1 ? "" : "(" + loops.ToString() + ")") :
                                        (loops == 1 ? "" : " - copy")
                                      );

                cont = IfValueMatchesInArray(directories, name + valueCompare);
                if (!cont) {
                    newName += valueCompare;
                }
                loops++;
            }

            return newName;
        }
        private static string GetFileName(string parentPath, string name, int type) {
            string newName = Path.GetFileNameWithoutExtension(name);

            string[] files = GetAllFiles(parentPath);

            for (int i = 0; i < files.Length; i++){
                files[i] = Path.GetFileName(files[i]);
            }

            bool cont = true;
            int loops = 1;
            while (cont){
                string valueCompare = (type == 0 ?
                                        (loops == 1 ? "" : "(" + loops.ToString() + ")") :
                                        (loops == 1 ? "" : " - copy")
                                      );

                cont = IfValueMatchesInArray(files, Path.GetFileNameWithoutExtension(name) + valueCompare + "." + GetExtension(name).ToLower());
                if (!cont){
                    newName += valueCompare + "." + GetExtension(name).ToLower();
                }
                loops++;
            }

            return newName;
        }
        private static string GetExtension(string name)
        {
            string extension = "";
            string[] array = name.Split('.');

            if (array.Length > 1)
            {
                extension = array[1].ToUpper();
            }

            return extension;
        }

        //Commands
        public static void CreateDirectory(string path) {
            LoadDocument();

            string xmlPath = PathToXml(Path.GetDirectoryName(path), 0);
            string xmlName = StringToHex(GetDirectoryName(Path.GetDirectoryName(path), Path.GetFileName(path), 0));

            XmlElement directory = document.CreateElement("directory");

            directory.SetAttribute("name", xmlName);
            directory.SetAttribute("modifdate", StringToHex(DateTime.Now.ToFileTime().ToString()));
            directory.SetAttribute("size", StringToHex("0"));

            document.SelectSingleNode(xmlPath).AppendChild(directory);

            SaveDocument();
        }
        public static void CreateFile(string path) {
            LoadDocument();

            string xmlPath = PathToXml(Path.GetDirectoryName(path), 0);
            string xmlName = StringToHex(GetFileName(Path.GetDirectoryName(path), Path.GetFileName(path), 0));

            XmlElement file = document.CreateElement("file");

            file.SetAttribute("name", xmlName);

            XmlElement extensionAtt = document.CreateElement("extension");
            extensionAtt.InnerText = GetExtension(Path.GetFileName(path));
            file.AppendChild(extensionAtt);

            XmlElement textAtt = document.CreateElement("text");
            textAtt.InnerText = "";
            file.AppendChild(textAtt);

            XmlElement modifdateAtt = document.CreateElement("modifdate");
            modifdateAtt.InnerText = DateTime.Now.ToFileTime().ToString();
            file.AppendChild(modifdateAtt);

            XmlElement sizeAtt = document.CreateElement("size");
            sizeAtt.InnerText = "0";
            file.AppendChild(sizeAtt);

            document.SelectSingleNode(xmlPath).AppendChild(file);

            SaveDocument();
        }

        public static string GetDirectoryAttributes(string path, string attribute) {
            LoadDocument();

            string xmlPath = PathToXml(path, 0);

            return HexToString(document.SelectSingleNode(xmlPath).Attributes[attribute].Value);
        }
        public static string GetFileAttributes(string path, string attribute) {
            LoadDocument();

            string xmlPath = PathToXml(path, 1);

            if (attribute == "name")
            {
                return HexToString(document.SelectSingleNode(xmlPath).Attributes[attribute].Value);
            }
            else {
                return document.SelectSingleNode(xmlPath + "/" + attribute).InnerText;
            }
        }

        public static void DeleteDirectory(string path) {
            if (!string.IsNullOrEmpty(path)) {
                LoadDocument();

                string xmlPath = PathToXml(path, 0);

                document.SelectSingleNode(xmlPath).ParentNode.RemoveChild(document.SelectSingleNode(xmlPath));

                SaveDocument();
            }
        }
        public static void DeleteFile(string path){
            LoadDocument();

            string xmlPath = PathToXml(path, 1);

            document.SelectSingleNode(xmlPath).ParentNode.RemoveChild(document.SelectSingleNode(xmlPath));

            SaveDocument();
        }

        public static void ChangeDirectoryAttribute(string path, string attribute, string value) {
            LoadDocument();

            string xmlPath = PathToXml(path, 0);

            document.SelectSingleNode(xmlPath).Attributes[attribute].Value = StringToHex(value);

            SaveDocument();
        }
        public static void ChangeFileAttribute(string path, string attribute, string value)
        {
            LoadDocument();

            string xmlPath = PathToXml(path, 1);

            if (attribute == "name")
            {
                document.SelectSingleNode(xmlPath).Attributes[attribute].Value = StringToHex(value);
            }
            else {
                document.SelectSingleNode(xmlPath + "/" + attribute).InnerText = value;
            }

            SaveDocument();
        }

        public static string[] GetAllDirectories(string path){
            LoadDocument();
        
            string xmlPath = PathToXml(path, 0);

            XmlNodeList directoryList = document.SelectNodes(xmlPath + "/directory");
            string[] directories = new string[directoryList.Count];
            for (int i = 0; i < directoryList.Count; i++) {
                directories[i] = path + "/" + HexToString(directoryList[i].Attributes["name"].Value);
            }

            return directories;
        }
        public static string[] GetAllFiles(string path)
        {
            LoadDocument();

            string xmlPath = PathToXml(path, 1);

            XmlNodeList fileList = document.SelectNodes(xmlPath + "/file");
            string[] files = new string[fileList.Count];

            for (int i = 0; i < fileList.Count; i++)
            {
                files[i] = path + "/" + HexToString(fileList[i].Attributes["name"].Value);
            }

            return files;
        }

        public static bool DirectoryExists(string path) {
            LoadDocument();

            string xmlPath = PathToXml(path, 0);

            if (document.SelectSingleNode(xmlPath) == null)
            {
                return false;
            }else {
                return true;
            }
        }
        public static bool FileExists(string path) {
            LoadDocument();

            string xmlPath = PathToXml(path, 1);

            if (document.SelectSingleNode(xmlPath) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void MoveDirectory(string directoryPath, string newParentPath) {
            LoadDocument();

            string xmlDirectoryPath = PathToXml(directoryPath, 0);
            string xmlNewParentPath = PathToXml(newParentPath, 0);

            XmlNode directoryNode = document.SelectSingleNode(xmlDirectoryPath).CloneNode(true);
            directoryNode.Attributes["name"].Value = StringToHex(GetDirectoryName(newParentPath, GetDirectoryAttributes(directoryPath, "name"), 0));
            document.SelectSingleNode(xmlNewParentPath).AppendChild(directoryNode);

            document.SelectSingleNode(xmlDirectoryPath).ParentNode.RemoveChild(document.SelectSingleNode(xmlDirectoryPath));

            SaveDocument();
        }
        public static void MoveFile(string filePath, string newParentPath)
        {
            LoadDocument();

            string xmlFilePath = PathToXml(filePath, 1);
            string xmlNewParentPath = PathToXml(newParentPath, 0);

            XmlNode fileNode = document.SelectSingleNode(xmlFilePath).CloneNode(true);
            fileNode.Attributes["name"].Value = StringToHex(GetFileName(newParentPath, GetFileAttributes(filePath, "name"), 0));
            document.SelectSingleNode(xmlNewParentPath).AppendChild(fileNode);

            document.SelectSingleNode(xmlFilePath).ParentNode.RemoveChild(document.SelectSingleNode(xmlFilePath));

            SaveDocument();
        }

        public static void CloneDirectory(string directoryPath, string newParentPath) {
            LoadDocument();

            string xmlDirectoryPath = PathToXml(directoryPath, 0);
            string xmlNewParentPath = PathToXml(newParentPath, 0);

            XmlNode directoryNode = document.SelectSingleNode(xmlDirectoryPath).CloneNode(true);
            directoryNode.Attributes["name"].Value = StringToHex(GetDirectoryName(newParentPath, GetDirectoryAttributes(directoryPath, "name"), 1));
            document.SelectSingleNode(xmlNewParentPath).AppendChild(directoryNode);

            SaveDocument();
        }
        public static void CloneFile(string filePath, string newParentPath)
        {
            LoadDocument();

            string xmlFilePath = PathToXml(filePath, 1);
            string xmlNewParentPath = PathToXml(newParentPath, 1);

            XmlNode fileNode = document.SelectSingleNode(xmlFilePath).CloneNode(true);
            fileNode.Attributes["name"].Value = StringToHex(GetFileName(newParentPath, GetFileAttributes(filePath, "name"), 1));
            document.SelectSingleNode(xmlNewParentPath).AppendChild(fileNode);

            SaveDocument();
        }
    }
}
