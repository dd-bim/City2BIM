using System.Globalization;

public class TranslationService
{
    private readonly ITranslator _translator;

    public TranslationService(ITranslator translator)
    {
        _translator = translator;
    }

    public string Translate(string key, CultureInfo culture)
    {
        return _translator.Translate(key, culture);
    }
}

public interface ITranslator
{
    string Translate(string key, CultureInfo culture);
}

