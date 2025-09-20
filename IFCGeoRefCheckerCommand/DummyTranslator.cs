using System.Globalization;
using IFCGeorefShared;

public class DummyTranslator : ITranslator
{
    public string Translate(string key, CultureInfo culture)
    {
        // Gibt einfach den Schlüssel zurück, keine Übersetzung
        return key;
    }
}