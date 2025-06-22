using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader 
{
    internal class PackageGroup : VisualElement
    {
        // Category Data
        private string GroupName { get; }
        private readonly List<Package> _packages;
        
        // Visual Elements
        private Button _groupExpanderBox;
        private VisualElement _groupContent;
        
        private Label _expanderLabel;
        private Label _groupLabel;
        
        // Other
        private Package _expandedPackage;
        
        private bool _expanded;
        private bool? _expandingOverriden;

        // Actions
        public Action<float> OnSliderChange;

        public PackageGroup(string groupName, bool createExpanded)
        {
            GroupName = groupName;
            AddToClassList("package-group");
            
            _packages = new List<Package>();
            _expanded = createExpanded;
            
            SetupSingleGroupElement();
            HandleExpanding();
        }

        public void AddPackage(Package package)
        {
            _packages.Add(package);
            _groupContent.Add(package);

            UpdateGroupLabel();
            package.OnPackageSelection = HandlePackageSelection;
            package.ShowFunctions(false);
        }

        public void SearchFilter(string filter)
        {
            var foundPackageCount = 0;
            foreach(var p in _packages)
            {
                if (p.SearchableText.Contains(filter))
                {
                    foundPackageCount++;
                    p.style.display = DisplayStyle.Flex;
                    _groupContent.style.display = DisplayStyle.Flex;
                }
                else
                    p.style.display = DisplayStyle.None;
            }

            if (string.IsNullOrEmpty(filter))
            {
                _expandingOverriden = null;
                
                UpdateGroupLabel();
                SetEnabled(true);
                HandleExpanding();
            }
            else
            {
                OverwriteGroupLabel($"{GroupName} ({foundPackageCount} found)");
                SetEnabled(foundPackageCount > 0);
                HandleExpanding(foundPackageCount > 0);
            }
        }

        private void SetupSingleGroupElement()
        {
            _groupExpanderBox = new Button();
            _groupExpanderBox.AddToClassList("group-expander-box");
            
            _expanderLabel = new Label { name = "ExpanderLabel", text = "►" };
            _expanderLabel.AddToClassList("expander");

            _groupLabel = new Label {text = $"{GroupName} ({_packages.Count})"};
            _groupLabel.AddToClassList("group-label");
            
            _groupExpanderBox.Add(_expanderLabel);
            _groupExpanderBox.Add(_groupLabel);

            _groupContent = new VisualElement {name = "GroupContentBox"};
            _groupContent.AddToClassList("group-content-box");

            _groupExpanderBox.clicked += () =>
            {
                if (_expandingOverriden == null)
                    _expanded = !_expanded;
                else
                    _expandingOverriden = !_expandingOverriden;

                HandleExpanding();
            };

            var groupSeparator = new VisualElement {name = "GroupSeparator"};
            groupSeparator.AddToClassList("group-separator");

            if (GroupName.ToLower() != "draft")
            {
                _groupLabel.SetEnabled(false);
                _groupContent.SetEnabled(false);
                groupSeparator.style.display = DisplayStyle.Flex;
            }

            Add(_groupExpanderBox);
            Add(_groupContent);
            Add(groupSeparator);
        }

        private void HandleExpanding(bool? overrideExpanding=null)
        {
            var expanded = _expanded;

            if (overrideExpanding != null)
            {
                expanded = (bool) overrideExpanding;
                _expandingOverriden = expanded;
            }
            else
            {
                if (_expandingOverriden != null)
                    expanded = (bool) _expandingOverriden;
            }
            
            _expanderLabel.text = !expanded ? "►" : "▼";
            var displayStyle = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            _groupContent.style.display = displayStyle;
        }

        private void HandlePackageSelection(Package package)
        {
            if (_expandedPackage == package)
            {
                _expandedPackage = null;
                return;
            }

            if (_expandedPackage == null)
            {
                _expandedPackage = package;
                return;
            }
            
            // Always where it was
            if (package.worldBound.y > _expandedPackage.worldBound.y)
            {
                var sliderChangeDelta = -(_expandedPackage.worldBound.height - package.worldBound.height);
                OnSliderChange?.Invoke(sliderChangeDelta);
            }
            
            _expandedPackage?.ShowFunctions(false);
            _expandedPackage = package;
            
        }

        private void UpdateGroupLabel()
        {
            _groupLabel.text = $"{GroupName} ({_packages.Count})";
        }

        private void OverwriteGroupLabel(string text)
        {
            _groupLabel.text = text;
        }
    }
}