﻿using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using EaServices.Files;
using EaServices.Functions;
using hoUtils.Package;


// ReSharper disable once CheckNamespace
namespace hoReverse.Services.AutoCpp
{
    public class AutoCpp
    {
        private static string dataSource = @"c:\Users\helmu_000\AppData\Roaming\Code\User\workspaceStorage\aa695e4b2b69e4df2595f987547a5da3\ms-vscode.cpptools\.BROWSE.VC.DB";
        //private static string dataSource = @"c:\Users\uidr5387\AppData\Roaming\Code\User\workspaceStorage\26045e663446b5f8d692303182313101\ms-vscode.cpptools\.BROWSE.VC.DB";

        private static string designRootPackageGuid = "{FB193821-8D09-438f-B63E-79BCA959C1CC}";
        private static string[] processFiles =
        {
            "Sens_pClu_Posn.c",
            "Sens_pCpu_T.c"
        };
        private readonly EA.Repository _rep;
        private readonly EA.Package _pkg;
        private readonly string _designPackagedIds;
        readonly List<string> _functionsNotFound = new List<string>();
        readonly Files _files;
        readonly Functions _functions;
        readonly Files _designFiles;
        readonly Functions _designFunctions;

        // statistics
        private int _deletedInterfaces = 0;
        private int _createdInterfaces = 0;


        /// <summary>
        /// Generate the modules. It updates the modules or put it into the selected package.
        /// </summary>
        /// <param name="rep"></param>
        /// <param name="pkg"></param>
        /// <returns></returns>
        public AutoCpp(EA.Repository rep, EA.Package pkg)
        {
            _rep = rep;
            _pkg = pkg;   
            // inventory from VC Code Database
            _files = new Files(rep);
            _designFiles = new Files(rep);
            _functions = new Functions(dataSource, Files,rep);
            _designFunctions = new Functions(_designFiles, rep);

            if (_rep.GetPackageByGuid(designRootPackageGuid) == null)
            {
                MessageBox.Show($@"Root package of existing design isnt't valid.
GUID={designRootPackageGuid}
Change variable: 'designRootPackageGuid=...'", "Cant inventory existing design, invalid root package");

            }
            _designPackagedIds = Package.GetBranch(_rep, "", _rep.GetPackageByGuid(designRootPackageGuid).PackageID);
            
        }
        /// <summary>
        /// Inventory Interfaces
        /// </summary>
        /// <returns></returns>
        public bool InventoryInterfaces()
        {
            Files.Inventory();
            return true;

        }
        //
        public bool InventoryDesignInterfaces()
        {
            // Get all interfaces
            // 'object_id' has to be included in select
            string sql = $@"select object_id, name
            from t_object 
            where object_type = 'Interface' AND
            package_id in ( { _designPackagedIds} )
            order by 2";
            EA.Collection interfaceList = _rep.GetElementSet(sql, 2);
            foreach (EA.Element el in interfaceList)
            {
                string name = el.Name;
                InterfaceItem ifItem = (InterfaceItem)_designFiles.Add($"{el.Name}.h");
                ifItem.El = el;

                foreach (EA.Method m in el.Methods)
                {
                    // create function
                    FunctionItem functionItem = new FunctionItem(m.Name, m.ReturnType, m.IsStatic, new List<ParameterItem>(), ifItem, m);
                    _designFunctions.FunctionList.Add(m.Name, functionItem);
                    // update interface
                    ifItem.ProvidedFunctions.Add(functionItem);
                }

                
            }

            // Show deleted Interface
            var deletedInterfaces = from s in _designFiles.FileList
                where ! _files.FileList.Any(t => (s.Value.Name == t.Value.Name)         && 
                                               t.Value.GetType().Name.Equals(typeof(InterfaceItem).Name ) )
                select s.Value.El;

            var createdInterfaces = from s in _files.FileList
                                    where ! _designFiles.FileList.All(t => (s.Value.Name == t.Value.Name)             &&
                                                   t.Value.GetType().Name.Equals(typeof(InterfaceItem).Name))
                                    select s.Value.El;

            _deletedInterfaces = 0;
            string deleteMe = "DeleteMe";
            foreach (var el in deletedInterfaces)
            {
                if (!el.Name.EndsWith(deleteMe))
                {
                    el.Name = el.Name + deleteMe;
                    el.Update();
                }
                _deletedInterfaces++;
            }
            _createdInterfaces = createdInterfaces.Count();
            
            return true;
        }



        /// <summary>
        /// Generate Interfaces
        /// </summary>
        /// <returns></returns>
        public int  GenerateInterfaces()
        {
           return Files.Generate(_pkg);

        }




        /// <summary>
        /// Inventory of files to process
        /// </summary>
        /// <returns></returns>
        public bool InventoryFiles()
        {
            // inventory modules
            foreach (string fileName in processFiles)
            {
                InventoryFile(fileName);
            }
            return true;
        }
        /// <summary>
        /// Generate all
        /// </summary>
        /// <returns></returns>
        public bool Generate()
        {
            // Generate modules
            foreach (string fileName in processFiles)
            {
                GenerateFile(fileName);
            }
            _rep.RefreshModelView(_pkg.PackageID);
            return true;
            return true;
        }
        /// <summary>
        /// Generate file
        /// </summary>
        /// <returns></returns>
        public bool Generate(string fileName)
        {
            return true;
        }
        /// <summary>
        /// Generate file according to list
        /// </summary>
        /// <returns></returns>
        public bool Generate(string[] fileNames)
        {
            return true;
        }

        /// <summary>
        /// Inventory a file (abc.h or abc.c) from it's content.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool InventoryFile(string fileName)
        {
            if (Files.FileList.ContainsKey(fileName.ToLower()))
            {
                FileItem fileItem = Files.FileList[fileName.ToLower()];
                if (fileItem is ModuleItem)
                {
                    ModuleItem moduleItem = (ModuleItem)fileItem;
                    moduleItem.Inventory();
                    moduleItem.InventoryRequiredFunctionsFromTextFile(moduleItem.FilePath, Functions, FunctionsNotFound);


                }
            }
            else
            {
                MessageBox.Show($@"The module '{fileName}' don't exists.", "Module file don't exists.");
            }

            return true;
        }
        /// <summary>
        /// Inventory a file (abc.h or abc.c) from it's content.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool GenerateFile(string fileName)
        {
            if (Files.FileList.ContainsKey(fileName.ToLower()))
            {
                FileItem fileItem = Files.FileList[fileName.ToLower()];
                if (fileItem is ModuleItem)
                {
                    ModuleItem moduleItem = (ModuleItem)fileItem;
                    moduleItem.Generate(_pkg);
                }
            }
            else
            {
                MessageBox.Show($@"The module '{fileName}' don't exists.", "Module file don't exists.");
            }

            return true;
        }

        public List<string> FunctionsNotFound { get => _functionsNotFound; }
        public Functions Functions { get => _functions;  }
        public Files Files { get => _files;  }

        public int DeletedInterfaces
        {
            get { return _deletedInterfaces; }
        }

        public int CreatedInterfaces
        {
            get { return _createdInterfaces; }
        }
    }
}