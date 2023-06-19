public class Product : IEquatable<Product?>
{

    public string? Id { get; set; }
    public string? ShortLinkYoula => Id != null ? $"https://youla.ru/p{Id}" : null;
    public string? ShortLinkVk { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PublishDate { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsSold { get; set; }
    public bool? IsExpired { get; set; }
    public bool? IsBlocked { get; set; }
    public bool? IsArchived { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsPromoted { get; set; }
    public User? Owner { get; set; }
    public string? UpdateDate { get; set; }
    public string? CreateDate { get; set; }

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
        if (this.GetType() == typeof(User))
            this.Equals(other as User);

        if (this.GetType() != other.GetType())
            return false;

        return this.Equals(other as Product);

    }
    public bool Equals(Product? other)
    {
        if (other == null)
            return false;

        //Здесь сравнение по ссылкам необязательно.
        //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
        if (object.ReferenceEquals(this, other))
            return true;


        return (Id, Owner?.Id) == (other.Id, other?.Owner?.Id);
    }

    public bool Equals(User? user)
    {
        if (user == null)
            return false;

        //Здесь сравнение по ссылкам необязательно.
        //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
        if (object.ReferenceEquals(Owner, user))
            return true;


        return Owner?.Id == user?.Id;
    }



    public static bool operator ==(Product? left, Product? right) => left?.Id == right?.Id || left?.Owner?.Id == right?.Owner?.Id;
    public static bool operator !=(Product? left, Product? right) => !(left == right);

    public override int GetHashCode() => (Id, Owner?.Id).GetHashCode();

}
