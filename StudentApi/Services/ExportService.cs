using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentApi.DTO;
using System.Data;
using System.Drawing;
using System.Text;
using Document = QuestPDF.Fluent.Document;

namespace StudentApi.Services
{
    public interface IExportService
    {
        ExportResponseDTO GeneratePdf(DataTable data, string reportTitle);
        ExportResponseDTO GenerateExcel(DataTable data, string reportTitle);
        ExportResponseDTO GenerateCsv(DataTable data, string reportTitle);
        ExportResponseDTO CreateEmptyPdf(string reportTitle);
        ExportResponseDTO CreateEmptyExcel(string reportTitle);
    }

    public class ExportService : IExportService
    {
        public ExportService()
        {
            // Set QuestPDF license (free for commercial use)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportResponseDTO GeneratePdf(DataTable data, string reportTitle)
        {
            if (data == null || data.Rows.Count == 0)
            {
                return CreateEmptyPdf(reportTitle);
            }

            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        page.Header()
                            .Text($"{reportTitle} Report")
                            .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Spacing(10);

                                // Add generation date
                                x.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                                // Create table
                                x.Item().Table(table =>
                                {
                                    // Define columns
                                    table.ColumnsDefinition(columns =>
                                    {
                                        for (int i = 0; i < data.Columns.Count; i++)
                                        {
                                            columns.RelativeColumn();
                                        }
                                    });

                                    // Add header
                                    table.Header(header =>
                                    {
                                        for (int i = 0; i < data.Columns.Count; i++)
                                        {
                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(5)
                                                .Text(data.Columns[i].ColumnName)
                                                .SemiBold();
                                        }
                                    });

                                    // Add data rows
                                    foreach (DataRow row in data.Rows)
                                    {
                                        for (int i = 0; i < data.Columns.Count; i++)
                                        {
                                            table.Cell()
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5)
                                                .Text(row[i]?.ToString() ?? "");
                                        }
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                using var memoryStream = new MemoryStream();
                document.GeneratePdf(memoryStream);

                return new ExportResponseDTO
                {
                    FileContent = memoryStream.ToArray(),
                    FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    ContentType = "application/pdf"
                };
            }
            catch (Exception ex)
            {
                // Fallback to CSV if PDF generation fails
                Console.WriteLine($"PDF generation failed: {ex.Message}");
                return GenerateCsv(data, reportTitle);
            }
        }

        public ExportResponseDTO GenerateExcel(DataTable data, string reportTitle)
        {
            if (data == null || data.Rows.Count == 0)
            {
                return CreateEmptyExcel(reportTitle);
            }

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(reportTitle);

                    // Add title
                    worksheet.Cell(1, 1).Value = $"{reportTitle} Report";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                    worksheet.Range(1, 1, 1, data.Columns.Count).Merge();

                    // Add generation date
                    worksheet.Cell(2, 1).Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    worksheet.Range(2, 1, 2, data.Columns.Count).Merge();

                    // Add headers starting from row 4
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        worksheet.Cell(4, i + 1).Value = data.Columns[i].ColumnName;
                        worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // Add data rows
                    for (int row = 0; row < data.Rows.Count; row++)
                    {
                        for (int col = 0; col < data.Columns.Count; col++)
                        {
                            worksheet.Cell(row + 5, col + 1).Value = data.Rows[row][col]?.ToString();
                        }
                    }

                    // Auto-fit columns and apply borders
                    worksheet.Columns().AdjustToContents();
                    worksheet.Range(4, 1, data.Rows.Count + 4, data.Columns.Count).Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return new ExportResponseDTO
                        {
                            FileContent = stream.ToArray(),
                            FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to CSV if Excel generation fails
                Console.WriteLine($"Excel generation failed: {ex.Message}");
                return GenerateCsv(data, reportTitle);
            }
        }

        public ExportResponseDTO CreateEmptyExcel(string reportTitle)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(reportTitle);
                    worksheet.Cell(1, 1).Value = $"No {reportTitle.ToLower()} data available for export";

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return new ExportResponseDTO
                        {
                            FileContent = memoryStream.ToArray(),
                            FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to simple text file
                Console.WriteLine($"Empty Excel creation failed: {ex.Message}");
                return new ExportResponseDTO
                {
                    FileContent = Encoding.UTF8.GetBytes($"No {reportTitle.ToLower()} data available for export"),
                    FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.txt",
                    ContentType = "text/plain"
                };
            }
        }

        public ExportResponseDTO GenerateCsv(DataTable data, string reportTitle)
        {
            if (data == null || data.Rows.Count == 0)
            {
                return new ExportResponseDTO
                {
                    FileContent = Encoding.UTF8.GetBytes("No data available for export"),
                    FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.csv",
                    ContentType = "text/csv"
                };
            }

            var sb = new StringBuilder();

            // Add headers
            for (int i = 0; i < data.Columns.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(EscapeCsvField(data.Columns[i].ColumnName));
            }
            sb.AppendLine();

            // Add data
            foreach (DataRow row in data.Rows)
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(EscapeCsvField(row[i]?.ToString() ?? ""));
                }
                sb.AppendLine();
            }

            return new ExportResponseDTO
            {
                FileContent = Encoding.UTF8.GetBytes(sb.ToString()),
                FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.csv",
                ContentType = "text/csv"
            };
        }

        public ExportResponseDTO CreateEmptyPdf(string reportTitle)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.Content()
                            .PaddingTop(5, Unit.Centimetre)
                            .AlignCenter()
                            .Text($"No {reportTitle.ToLower()} data available for export")
                            .FontSize(16);
                    });
                });

                using var memoryStream = new MemoryStream();
                document.GeneratePdf(memoryStream);

                return new ExportResponseDTO
                {
                    FileContent = memoryStream.ToArray(),
                    FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    ContentType = "application/pdf"
                };
            }
            catch (Exception ex)
            {
                // Fallback to text file
                Console.WriteLine($"Empty PDF creation failed: {ex.Message}");
                return new ExportResponseDTO
                {
                    FileContent = Encoding.UTF8.GetBytes($"No {reportTitle.ToLower()} data available for export"),
                    FileName = $"{reportTitle.ToLower()}_report_{DateTime.Now:yyyyMMddHHmmss}.txt",
                    ContentType = "text/plain"
                };
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}