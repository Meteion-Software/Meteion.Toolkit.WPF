This namespace contains some common value converters.

Value converters in this folder should: 
- Implement the IValueConverter interface.
- Be decorated with the ValueConversionAttribute.
- Contain the line ``public static readonly {ClassName} Instance = new {ClassName}();`` in the class body to allow for easy access to a single instance of the converter.
- Return DependencyProperty.UnsetValue when the conversion cannot be performed.