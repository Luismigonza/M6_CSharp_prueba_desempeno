using RentaSegura.Web.Domain;
using Xunit;

namespace RentaSegura.UnitTests;

public class ReservationTests
{
    private static Property SampleProperty(decimal price = 100_000m) => new()
    {
        Id = Guid.NewGuid(), OwnerId = "owner-1",
        Title = "Test", Description = "desc", City = "Medellín", Address = "Calle 1",
        PricePerNight = price, Bedrooms = 2, Capacity = 4
    };

    // -- Creación 

    [Fact]
    public void Create_AplicaHorariosFijos_14y12()
    {
        var checkIn  = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5));
        var checkOut = checkIn.AddDays(3);

        var (r, err) = Reservation.Create(SampleProperty(), "g-1", checkIn, checkOut);

        Assert.Null(err);
        Assert.Equal(new TimeOnly(14, 0), TimeOnly.FromDateTime(r!.CheckInDateTime));
        Assert.Equal(new TimeOnly(12, 0), TimeOnly.FromDateTime(r!.CheckOutDateTime));
    }

    [Fact]
    public void Create_CalculaNochesYPrecio_Correctamente()
    {
        var checkIn  = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10));
        var checkOut = checkIn.AddDays(4);

        var (r, _) = Reservation.Create(SampleProperty(150_000m), "g-1", checkIn, checkOut);

        Assert.Equal(4,         r!.Nights);
        Assert.Equal(600_000m,  r.PricePaid);
    }

    [Fact]
    public void Create_RechazaSalida_MenorOIgualQueLlegada()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5));
        var (r, err) = Reservation.Create(SampleProperty(), "g-1", checkIn, checkIn);

        Assert.Null(r);
        Assert.NotNull(err);
    }

    [Fact]
    public void Create_RechazaFecha_EnElPasado()
    {
        var checkIn  = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var checkOut = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2));
        var (r, err) = Reservation.Create(SampleProperty(), "g-1", checkIn, checkOut);

        Assert.Null(r);
        Assert.NotNull(err);
    }

    // -- Política de cancelación 

    [Fact]
    public void CanCancel_PermiteConMasDe48H_DeAntelacion()
    {
        var r = BuildConfirmed(daysUntilCheckIn: 5);
        var (ok, reason) = r.CanCancel();
        Assert.True(ok);
        Assert.Null(reason);
    }

    [Fact]
    public void CanCancel_DeniegaCon24H_DeAntelacion()
    {
        var r = BuildConfirmed(daysUntilCheckIn: 1);
        var (ok, _) = r.CanCancel();
        Assert.False(ok);
    }

    [Fact]
    public void CanCancel_DeniegaSiYaEstaCancelada()
    {
        var r = BuildConfirmed(daysUntilCheckIn: 10);
        r.Status = ReservationStatus.Cancelled;
        var (ok, _) = r.CanCancel();
        Assert.False(ok);
    }

    [Fact]
    public void CanCancel_DeniegaSiEstaCompletada()
    {
        var r = BuildConfirmed(daysUntilCheckIn: 10);
        r.Status = ReservationStatus.Completed;
        var (ok, _) = r.CanCancel();
        Assert.False(ok);
    }

    // -- Helpers 

    private static Reservation BuildConfirmed(int daysUntilCheckIn)
    {
        var checkIn  = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(daysUntilCheckIn));
        var checkOut = checkIn.AddDays(2);
        var (r, _)   = Reservation.Create(SampleProperty(), "g-1", checkIn, checkOut);
        return r!;
    }
}