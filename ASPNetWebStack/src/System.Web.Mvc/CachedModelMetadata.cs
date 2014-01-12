﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc
{
    // This class assumes that model metadata is expensive to create, and allows the user to
    // stash a cache object that can be copied around as a prototype to make creation and
    // computation quicker. It delegates the retrieval of values to getter methods, the results
    // of which are cached on a per-metadata-instance basis.
    //
    // This allows flexible caching strategies: either caching the source of information across
    // instances or caching of the actual information itself, depending on what the developer
    // decides to put into the prototype cache.
    public abstract class CachedModelMetadata<TPrototypeCache> : ModelMetadata
    {
        private bool _convertEmptyStringToNull;
        private string _dataTypeName;
        private string _description;
        private string _displayFormatString;
        private string _displayName;
        private string _editFormatString;
        private bool _hideSurroundingHtml;
        private bool _isReadOnly;
        private bool _isRequired;
        private string _nullDisplayText;
        private int _order;
        private string _shortDisplayName;
        private bool _showForDisplay;
        private bool _showForEdit;
        private string _templateHint;
        private string _watermark;

        private bool _convertEmptyStringToNullComputed;
        private bool _dataTypeNameComputed;
        private bool _descriptionComputed;
        private bool _displayFormatStringComputed;
        private bool _displayNameComputed;
        private bool _editFormatStringComputed;
        private bool _hideSurroundingHtmlComputed;
        private bool _isReadOnlyComputed;
        private bool _isRequiredComputed;
        private bool _nullDisplayTextComputed;
        private bool _orderComputed;
        private bool _shortDisplayNameComputed;
        private bool _showForDisplayComputed;
        private bool _showForEditComputed;
        private bool _templateHintComputed;
        private bool _watermarkComputed;

        // Constructor for creating real instances of the metadata class based on a prototype
        protected CachedModelMetadata(CachedModelMetadata<TPrototypeCache> prototype, Func<object> modelAccessor)
            : base(prototype.Provider, prototype.ContainerType, modelAccessor, prototype.ModelType, prototype.PropertyName)
        {
            PrototypeCache = prototype.PrototypeCache;
        }

        // Constructor for creating the prototype instances of the metadata class
        protected CachedModelMetadata(CachedDataAnnotationsModelMetadataProvider provider, Type containerType, Type modelType, string propertyName, TPrototypeCache prototypeCache)
            : base(provider, containerType, null /* modelAccessor */, modelType, propertyName)
        {
            PrototypeCache = prototypeCache;
        }

        public sealed override bool ConvertEmptyStringToNull
        {
            get
            {
                if (!_convertEmptyStringToNullComputed)
                {
                    _convertEmptyStringToNull = ComputeConvertEmptyStringToNull();
                    _convertEmptyStringToNullComputed = true;
                }
                return _convertEmptyStringToNull;
            }
            set
            {
                _convertEmptyStringToNull = value;
                _convertEmptyStringToNullComputed = true;
            }
        }

        public sealed override string DataTypeName
        {
            get
            {
                if (!_dataTypeNameComputed)
                {
                    _dataTypeName = ComputeDataTypeName();
                    _dataTypeNameComputed = true;
                }
                return _dataTypeName;
            }
            set
            {
                _dataTypeName = value;
                _dataTypeNameComputed = true;
            }
        }

        public sealed override string Description
        {
            get
            {
                if (!_descriptionComputed)
                {
                    _description = ComputeDescription();
                    _descriptionComputed = true;
                }
                return _description;
            }
            set
            {
                _description = value;
                _descriptionComputed = true;
            }
        }

        public sealed override string DisplayFormatString
        {
            get
            {
                if (!_displayFormatStringComputed)
                {
                    _displayFormatString = ComputeDisplayFormatString();
                    _displayFormatStringComputed = true;
                }
                return _displayFormatString;
            }
            set
            {
                _displayFormatString = value;
                _displayFormatStringComputed = true;
            }
        }

        public sealed override string DisplayName
        {
            get
            {
                if (!_displayNameComputed)
                {
                    _displayName = ComputeDisplayName();
                    _displayNameComputed = true;
                }
                return _displayName;
            }
            set
            {
                _displayName = value;
                _displayNameComputed = true;
            }
        }

        public sealed override string EditFormatString
        {
            get
            {
                if (!_editFormatStringComputed)
                {
                    _editFormatString = ComputeEditFormatString();
                    _editFormatStringComputed = true;
                }
                return _editFormatString;
            }
            set
            {
                _editFormatString = value;
                _editFormatStringComputed = true;
            }
        }

        public sealed override bool HideSurroundingHtml
        {
            get
            {
                if (!_hideSurroundingHtmlComputed)
                {
                    _hideSurroundingHtml = ComputeHideSurroundingHtml();
                    _hideSurroundingHtmlComputed = true;
                }
                return _hideSurroundingHtml;
            }
            set
            {
                _hideSurroundingHtml = value;
                _hideSurroundingHtmlComputed = true;
            }
        }

        public sealed override bool IsReadOnly
        {
            get
            {
                if (!_isReadOnlyComputed)
                {
                    _isReadOnly = ComputeIsReadOnly();
                    _isReadOnlyComputed = true;
                }
                return _isReadOnly;
            }
            set
            {
                _isReadOnly = value;
                _isReadOnlyComputed = true;
            }
        }

        public sealed override bool IsRequired
        {
            get
            {
                if (!_isRequiredComputed)
                {
                    _isRequired = ComputeIsRequired();
                    _isRequiredComputed = true;
                }
                return _isRequired;
            }
            set
            {
                _isRequired = value;
                _isRequiredComputed = true;
            }
        }

        public sealed override string NullDisplayText
        {
            get
            {
                if (!_nullDisplayTextComputed)
                {
                    _nullDisplayText = ComputeNullDisplayText();
                    _nullDisplayTextComputed = true;
                }
                return _nullDisplayText;
            }
            set
            {
                _nullDisplayText = value;
                _nullDisplayTextComputed = true;
            }
        }

        public sealed override int Order
        {
            get
            {
                if (!_orderComputed)
                {
                    _order = ComputeOrder();
                    _orderComputed = true;
                }
                return _order;
            }
            set
            {
                _order = value;
                _orderComputed = true;
            }
        }

        protected TPrototypeCache PrototypeCache { get; set; }

        public sealed override string ShortDisplayName
        {
            get
            {
                if (!_shortDisplayNameComputed)
                {
                    _shortDisplayName = ComputeShortDisplayName();
                    _shortDisplayNameComputed = true;
                }
                return _shortDisplayName;
            }
            set
            {
                _shortDisplayName = value;
                _shortDisplayNameComputed = true;
            }
        }

        public sealed override bool ShowForDisplay
        {
            get
            {
                if (!_showForDisplayComputed)
                {
                    _showForDisplay = ComputeShowForDisplay();
                    _showForDisplayComputed = true;
                }
                return _showForDisplay;
            }
            set
            {
                _showForDisplay = value;
                _showForDisplayComputed = true;
            }
        }

        public sealed override bool ShowForEdit
        {
            get
            {
                if (!_showForEditComputed)
                {
                    _showForEdit = ComputeShowForEdit();
                    _showForEditComputed = true;
                }
                return _showForEdit;
            }
            set
            {
                _showForEdit = value;
                _showForEditComputed = true;
            }
        }

        public sealed override string SimpleDisplayText
        {
            get
            {
                // This is already cached in the base class with an appropriate override available
                return base.SimpleDisplayText;
            }
            set { base.SimpleDisplayText = value; }
        }

        public sealed override string TemplateHint
        {
            get
            {
                if (!_templateHintComputed)
                {
                    _templateHint = ComputeTemplateHint();
                    _templateHintComputed = true;
                }
                return _templateHint;
            }
            set
            {
                _templateHint = value;
                _templateHintComputed = true;
            }
        }

        public sealed override string Watermark
        {
            get
            {
                if (!_watermarkComputed)
                {
                    _watermark = ComputeWatermark();
                    _watermarkComputed = true;
                }
                return _watermark;
            }
            set
            {
                _watermark = value;
                _watermarkComputed = true;
            }
        }

        protected virtual bool ComputeConvertEmptyStringToNull()
        {
            return base.ConvertEmptyStringToNull;
        }

        protected virtual string ComputeDataTypeName()
        {
            return base.DataTypeName;
        }

        protected virtual string ComputeDescription()
        {
            return base.Description;
        }

        protected virtual string ComputeDisplayFormatString()
        {
            return base.DisplayFormatString;
        }

        protected virtual string ComputeDisplayName()
        {
            return base.DisplayName;
        }

        protected virtual string ComputeEditFormatString()
        {
            return base.EditFormatString;
        }

        protected virtual bool ComputeHideSurroundingHtml()
        {
            return base.HideSurroundingHtml;
        }

        protected virtual bool ComputeIsReadOnly()
        {
            return base.IsReadOnly;
        }

        protected virtual bool ComputeIsRequired()
        {
            return base.IsRequired;
        }

        protected virtual string ComputeNullDisplayText()
        {
            return base.NullDisplayText;
        }

        protected virtual int ComputeOrder()
        {
            return base.Order;
        }

        protected virtual string ComputeShortDisplayName()
        {
            return base.ShortDisplayName;
        }

        protected virtual bool ComputeShowForDisplay()
        {
            return base.ShowForDisplay;
        }

        protected virtual bool ComputeShowForEdit()
        {
            return base.ShowForEdit;
        }

        protected virtual string ComputeSimpleDisplayText()
        {
            return base.GetSimpleDisplayText();
        }

        protected virtual string ComputeTemplateHint()
        {
            return base.TemplateHint;
        }

        protected virtual string ComputeWatermark()
        {
            return base.Watermark;
        }

        protected sealed override string GetSimpleDisplayText()
        {
            // Rename for consistency
            return ComputeSimpleDisplayText();
        }
    }
}
