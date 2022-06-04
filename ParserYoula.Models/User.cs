using System.ComponentModel.DataAnnotations.Schema;

public class User : IEquatable<User?>
{
    /// <summary>
    /// ID
    /// </summary>
    [Column("user_id")]
    public string? Id { get; set; }
    /// <summary>
    /// Имя
    /// </summary>
    [NotMapped]
    public string? Name { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    [NotMapped]
    public string? Phone { get; set; }

    /// <summary>
    /// Разрешенное время звонка
    /// </summary>
    [NotMapped]
    public string? CallTime { get; set; }
    
    /// <summary>
    /// Это магазин
    /// </summary>
    [NotMapped]
    public bool? IsShop { get; set; }

    /// <summary>
    /// Чат недоступен
    /// </summary>
    [NotMapped]
    public bool? IsChatLocked { get; set; }

    /// <summary>
    /// Телефонные звонки недоступны
    /// </summary>
    [NotMapped]
    public bool? IsPhoneLocked { get; set; }
    /// <summary>
    /// Телефонные звонки отключены
    /// </summary>
    [NotMapped]
    public bool? IsPhoneDisabled { get; set; }

    /// <summary>
    /// Причина отключенных звонков
    /// </summary>
    [NotMapped]
    public string? DisableCallAlertText { get; set; }

    [NotMapped]
    public int? RatingMarkCount { get; set; }

    [NotMapped]
    public bool? AnyCallEnabled { get; set; }
    [NotMapped]
    public bool? SystemCallEnabled { get; set; }
    [NotMapped]
    public bool? P2pCallEnabled { get; set; }

    public override bool Equals(object? other)
    {
        //Последовательность проверки должна быть именно такой.
        //Если не проверить на null объект other, то other.GetType() может выбросить //NullReferenceException.            
        if (other == null)
            return false;

        //Если ссылки указывают на один и тот же адрес, то их идентичность гарантирована.
        if (object.ReferenceEquals(this, other))
            return true;

        //Если класс находится на вершине иерархии или просто не имеет наследников, то можно просто
        //сделать Vehicle tmp = other as Vehicle; if(tmp==null) return false; 
        //Затем вызвать экземплярный метод, сразу передав ему объект tmp.
        if (this.GetType() != other.GetType())
            return false;

        return this.Equals(other as User);

    }
    public bool Equals(User? other)
    {
        if (other == null)
            return false;

        //Здесь сравнение по ссылкам необязательно.
        //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
        if (object.ReferenceEquals(this, other))
            return true;

        return other.Id == this.Id;
    }
    public static bool operator ==(User? left, User? right) => left?.Id == right?.Id;
    public static bool operator !=(User? left, User? right) => !(left == right);
    public override int GetHashCode() => HashCode.Combine(Id);


}
