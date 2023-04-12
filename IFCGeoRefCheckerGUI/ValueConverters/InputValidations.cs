using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    public class LatLonRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public LatLonRule()
        {

        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double angle = 0.0;

            try
            {
                if (((string)value).Length > 0) { angle = Double.Parse((string)value, cultureInfo); }
            }
            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if ((angle < Min) || (angle > Max))
            {
                return new ValidationResult(false, $"Please enter an decimal angle between {Min} and {Max}");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class InputValidator
    {
        public static bool IsValid(DependencyObject parent)
        {
            /*
            return !Validation.GetHasError(parent) &&
                LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>().All(IsValid);
            
            
            bool valid = true;

            LocalValueEnumerator localValues = parent.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                if (BindingOperations.IsDataBound(parent, entry.Property))
                {
                    Binding binding = BindingOperations.GetBinding(parent, entry.Property);
                    foreach (ValidationRule rule in binding.ValidationRules)
                    {
                        ValidationResult result = rule.Validate(parent.GetValue(entry.Property), null);
                        if (result!.IsValid)
                        {
                            BindingExpression expression = BindingOperations.GetBindingExpression(parent, entry.Property);
                            System.Windows.Controls.Validation.MarkInvalid(expression, new ValidationError(rule, expression, result.ErrorContent, null));
                        }
                    }
                }
            }

            /*for (int i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!IsValid(child)) { valid = false; return valid; }
            }*/
            bool valid = true;

            LocalValueEnumerator localValues = parent.GetLocalValueEnumerator();
            while(localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                if (BindingOperations.IsDataBound(parent, entry.Property))
                {
                    Binding binding = BindingOperations.GetBinding(parent, entry.Property);
                    if (binding.ValidationRules.Count > 0)
                    {
                        BindingExpression expression = BindingOperations.GetBindingExpression(parent, entry.Property);
                        expression.UpdateSource();

                        if (expression.HasError)
                        {
                            valid = false;
                        }
                    }
                }
            }

            System.Collections.IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object obj in children)
            {
                if (obj is DependencyObject)
                {
                    DependencyObject child = (DependencyObject)obj;
                    if (!IsValid(child)) 
                    { 
                        return false; 
                    }
                }
            }

            return valid;
            //*/
        }
    }
    

    public class ValidationHelper: INotifyDataErrorInfo
    {
        readonly IDictionary<string, List<string>> errorList = new Dictionary<string, List<string>>();

        public string this[string propertyName]
        {
            get
            {
                if (errorList.ContainsKey(propertyName))
                {
                    return errorList[propertyName].First();
                }
                return String.Empty;
            }
        }

        public bool HasErrors => errorList.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public void ClearError([CallerMemberName] string property = "")
        {
            if (errorList.Remove(property))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
            }
            
        }
        public void AddError(string errorMessage, [CallerMemberName] string property = "")
        {
            if (!errorList.ContainsKey(property))
            {
                errorList.Add(property, new List<string>());
            }
            errorList[property].Add(errorMessage);
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (errorList.ContainsKey(propertyName!))
            {
                return errorList[propertyName!];
            }
            return Array.Empty<string>();
        }

    }

}
