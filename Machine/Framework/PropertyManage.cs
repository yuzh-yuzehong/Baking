﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Machine
{
    //属性类
    public class Property
    {
        private string _name = string.Empty;            // 属性名
        private object _value = null;                   // 属性值
        private int _permissions = -1;                  // 属性权限
        private string _displayName = string.Empty;     // 显示名称
        private string _description = string.Empty;     // 属性详细描述
        private string _category = string.Empty;        // 属性分组类别

        private bool _readonly = false;                 // 属性仅可读
        private bool _visible = true;                   // 属性可见性
        TypeConverter _converter = null;                // 转换器
        object _editor = null;                          // 属性编辑器

        /// <summary>
        /// 添加属性参数
        /// </summary>
        /// <param name="categroy">属性分组类别</param>
        /// <param name="name">属性名</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="permissions">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        public Property(string categroy, string name, string displayName, string description, object value, int permissions, bool readOnly = false, bool visible = true)
        {
            this._category = categroy;
            this._name = name;
            this._value = value;
            this._permissions = permissions;
            this._displayName = displayName;
            this._description = description;

            this._readonly = readOnly;
            this._visible = visible;
        }

        public string Name  // 获得属性名
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public string DisplayName   // 属性显示名称
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }
        public string Category  // 属性所属类别
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
            }
        }
        public object Value  // 属性值
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        public int Permissions      // 属性权限，控制参数可编辑权限
        {
            get
            {
                return _permissions;
            }

            set
            {
                _permissions = value;
            }
        }
        public string Description   // 属性描述，显示参数说明
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }
        public TypeConverter Converter  // 类型转换器，我们在制作下拉列表时需要用到
        {
            get
            {
                return _converter;
            }
            set
            {
                _converter = value;
            }
        }
        public bool ReadOnly  // 是否为只读属性
        {
            get
            {
                return _readonly;
            }
            set
            {
                _readonly = value;
            }
        }
        public bool Visible  // 是否可见
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
            }
        }
        public virtual object Editor   // 属性编辑器
        {
            get
            {
                return _editor;
            }
            set
            {
                _editor = value;
            }
        }

    }

    public class PropertyManage : CollectionBase, ICustomTypeDescriptor
    {
        public void Add(Property value)
        {
            int flag = -1;
            if(value != null)
            {
                if(base.List.Count > 0)
                {
                    IList<Property> mList = new List<Property>();
                    for(int i = 0; i < base.List.Count; i++)
                    {
                        Property p = base.List[i] as Property;
                        if(value.Name == p.Name)
                        {
                            flag = i;
                        }
                        mList.Add(p);
                    }
                    if(flag == -1)
                    {
                        mList.Add(value);
                    }
                    base.List.Clear();
                    foreach(Property p in mList)
                    {
                        base.List.Add(p);
                    }
                }
                else
                {
                    base.List.Add(value);
                }
            }
        }
        public void Remove(Property value)
        {
            if(value != null && base.List.Count > 0)
                base.List.Remove(value);
        }
        public Property this[int index]
        {
            get
            {
                return (Property)base.List[index];
            }
            set
            {
                base.List[index] = (Property)value;
            }
        }

        #region ICustomTypeDescriptor 成员
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }
        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }
        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }
        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }
        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }
        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }
        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }
        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptor[] newProps = new PropertyDescriptor[this.Count];
            for(int i = 0; i < this.Count; i++)
            {
                Property prop = (Property)this[i];
                newProps[i] = new CustomPropertyDescriptor(ref prop, attributes);
            }
            return new PropertyDescriptorCollection(newProps);
        }
        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
        #endregion

        private class CustomPropertyDescriptor : PropertyDescriptor
        {
            Property m_Property;
            public CustomPropertyDescriptor(ref Property myProperty, Attribute[] attrs)
                : base(myProperty.Name, attrs)
            {
                m_Property = myProperty;
            }
            #region PropertyDescriptor 重写方法
            public override bool CanResetValue(object component)
            {
                return false;
            }
            public override Type ComponentType
            {
                get
                {
                    return null;
                }
            }
            public override object GetValue(object component)
            {
                return m_Property.Value;
            }
            public override string Description
            {
                get
                {
                    return m_Property.Name + "\r\n" + m_Property.Description;
                }
            }
            public override string Category
            {
                get
                {
                    return m_Property.Category;
                }
            }
            public override string DisplayName
            {
                get
                {
                    return ("" != m_Property.DisplayName) ? m_Property.DisplayName : m_Property.Name;
                }
            }
            public override bool IsReadOnly
            {
                get
                {
                    return m_Property.ReadOnly;
                }
            }
            public override void ResetValue(object component)
            {
                //Have to implement
            }
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
            public override void SetValue(object component, object value)
            {
                m_Property.Value = value;
            }
            public override TypeConverter Converter
            {
                get
                {
                    return m_Property.Converter;
                }
            }
            public override Type PropertyType
            {
                get { return m_Property.Value.GetType(); }
            }
            public override object GetEditor(Type editorBaseType)
            {
                return m_Property.Editor == null ? base.GetEditor(editorBaseType) : m_Property.Editor;
            }
            #endregion
        }
    }
}
