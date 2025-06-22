using System.Collections.Generic;

namespace AssetStoreTools.Uploader
{
    public static class PackageStorer
    {
        private static readonly Dictionary<string, Package> SavedPackages = new Dictionary<string, Package>();

        public static Package GetPackage(string packageId, string versionId, string packageName,
            string status, string category, string lastDate, string lastSize, bool isCompleteProject)
        {
            if (SavedPackages.ContainsKey(versionId))
            {
                // Update data fields in case of changes
                SavedPackages[versionId].UpdateDataValues(packageName, status, category, lastDate, lastSize, isCompleteProject);
                return SavedPackages[versionId];
            }
            
            var package = new Package(packageId, versionId, packageName, status, category, lastDate, lastSize, isCompleteProject);
            SavedPackages.Add(versionId, package);
            return package;
        }

        public static void Reset()
        {
            SavedPackages.Clear();
        }
    }
}