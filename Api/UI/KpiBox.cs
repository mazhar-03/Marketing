using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Api.UI;

public class KpiBox : IComponent
{
    private readonly string _title;
    private readonly string _value;

    public KpiBox(string title, string value)
    {
        _title = title;
        _value = value;
    }

    public void Compose(IContainer container)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(col =>
            {
                col.Item().Text(_title)
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);

                col.Item().Text(_value)
                    .FontSize(16)
                    .Bold();
            });
    }
}