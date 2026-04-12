namespace App.Contracts;

using Base.Domain;

public interface ILookUpEntity
{
    string Code {get; set;}

    LangStr Label {get; set;}
    
}
