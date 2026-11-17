using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BookingService.DTOs;
using BookingService.Services.TicketPdfGeneratorService;

namespace BookingService.Services.TicketPdfGeneratorService;

public class TicketPdfGenerator : ITicketPdfGenerator
{
    public byte[] GenerateTicketBytes(
        BookingResponseDto booking,
        string trainName,
        string source,
        string destination)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x =>
                    x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item()
                            .Text("INDIAN RAILWAYS")
                            .FontSize(22)
                            .Bold();

                        column.Item()
                            .Text("Electronic Reservation Slip (ERS)")
                            .FontSize(10)
                            .Italic();
                    });

                    row.ConstantItem(100)
                        .AlignRight()
                        .Text("RAILWAY TICKET")
                        .Bold()
                        .FontSize(10);
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    // PNR and Status
                    column.Item()
                        .Border(1)
                        .Padding(12)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"PNR: {booking.PNR}")
                                .Bold()
                                .FontSize(14);

                            row.RelativeItem()
                                .AlignRight()
                                .Text(booking.Status.ToUpper())
                                .Bold()
                                .FontSize(12)
                                .FontColor(
                                    booking.Status.Equals(
                                        "Confirmed",
                                        StringComparison.OrdinalIgnoreCase)
                                        ? Colors.Green.Darken2
                                        : Colors.Red.Darken2);
                        });

                    column.Item().PaddingTop(15);

                    // Journey details
                    column.Item()
                        .Border(1)
                        .Padding(12)
                        .Column(details =>
                        {
                            details.Item()
                                .Text("JOURNEY DETAILS")
                                .Bold()
                                .FontSize(12);

                            details.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("FROM").Bold();
                                    c.Item().Text(source).FontSize(13);
                                });

                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("TO").Bold();
                                    c.Item().Text(destination).FontSize(13);
                                });
                            });

                            details.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("TRAIN").Bold();
                                    c.Item().Text(trainName);
                                });

                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("CLASS").Bold();
                                    c.Item().Text(booking.ClassType);
                                });
                            });

                            details.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("JOURNEY DATE").Bold();
                                    c.Item().Text(
                                        booking.JourneyDate
                                            .ToString("dd MMM yyyy"));
                                });

                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("BOOKING DATE").Bold();
                                    c.Item().Text(
                                        booking.BookingDate
                                            .ToString("dd MMM yyyy"));
                                });
                            });
                        });

                    column.Item().PaddingTop(15);

                    // Fare summary
                    column.Item()
                        .Border(1)
                        .Padding(12)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"NUMBER OF TICKETS: {booking.NumberOfTickets}")
                                .Bold();

                            row.RelativeItem()
                                .AlignRight()
                                .Text($"TOTAL FARE: ₹{booking.TotalFare:N2}")
                                .Bold()
                                .FontSize(12);
                        });

                    column.Item().PaddingTop(15);

                    // Passenger table
                    column.Item()
                        .Text("PASSENGER DETAILS")
                        .Bold()
                        .FontSize(12);

                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(50);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell)
                                .Text("#");

                            header.Cell().Element(HeaderCell)
                                .Text("Name");

                            header.Cell().Element(HeaderCell)
                                .Text("Age");

                            header.Cell().Element(HeaderCell)
                                .Text("Gender");

                            header.Cell().Element(HeaderCell)
                                .Text("Seat ID");
                        });

                        int number = 1;

                        foreach (var passenger in booking.Passengers)
                        {
                            table.Cell().Element(BodyCell)
                                .Text(number.ToString());

                            table.Cell().Element(BodyCell)
                                .Text(passenger.Name);

                            table.Cell().Element(BodyCell)
                                .Text(passenger.Age.ToString());

                            table.Cell().Element(BodyCell)
                                .Text(passenger.Gender);

                            table.Cell().Element(BodyCell)
                                .Text(passenger.SeatId.ToString());

                            number++;
                        }
                    });

                    column.Item().PaddingTop(20);

                    column.Item()
                        .Text("This is a computer-generated Electronic Reservation Slip.")
                        .FontSize(8)
                        .Italic();
                });

                page.Footer()
                    .AlignCenter()
                    .Text("Railway Reservation System")
                    .FontSize(8);
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten2)
            .Border(1)
            .Padding(5)
            .AlignCenter();
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .Border(1)
            .Padding(5);
    }
}