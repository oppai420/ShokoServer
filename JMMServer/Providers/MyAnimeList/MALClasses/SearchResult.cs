﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :2.0.50727.4016
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;
using Shoko.Models;

// 
// Ce code source a été automatiquement généré par xsd, Version=2.0.50727.3038.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class anime {
    
    private animeEntry[] entryField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("entry")]
    public animeEntry[] entry {
        get {
            return this.entryField;
        }
        set {
            this.entryField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class animeEntry {
    
    private int idField;
    
    private string titleField;
    
    private string englishField;
    
    private string synonymsField;
    
    private int episodesField;
    
    private decimal scoreField;
    
    private string typeField;
    
    private string statusField;
    
    private string start_dateField;
    
    private string end_dateField;
    
    private string synopsisField;
    
    private string imageField;
    
    /// <remarks/>
    public int id {
        get {
            return this.idField;
        }
        set {
            this.idField = value;
        }
    }
    
    /// <remarks/>
    public string title {
        get {
            return this.titleField;
        }
        set {
            this.titleField = value;
        }
    }
    
    /// <remarks/>
    public string english {
        get {
            return this.englishField;
        }
        set {
            this.englishField = value;
        }
    }
    
    /// <remarks/>
    public string synonyms {
        get {
            return this.synonymsField;
        }
        set {
            this.synonymsField = value;
        }
    }
    
    /// <remarks/>
    public int episodes {
        get {
            return this.episodesField;
        }
        set {
            this.episodesField = value;
        }
    }
    
    /// <remarks/>
    public decimal score {
        get {
            return this.scoreField;
        }
        set {
            this.scoreField = value;
        }
    }
    
    /// <remarks/>
    public string type {
        get {
            return this.typeField;
        }
        set {
            this.typeField = value;
        }
    }
    
    /// <remarks/>
    public string status {
        get {
            return this.statusField;
        }
        set {
            this.statusField = value;
        }
    }
    
    /// <remarks/>
    public string start_date {
        get {
            return this.start_dateField;
        }
        set {
            this.start_dateField = value;
        }
    }
    
    /// <remarks/>
    public string end_date {
        get {
            return this.end_dateField;
        }
        set {
            this.end_dateField = value;
        }
    }
    
    /// <remarks/>
    public string synopsis {
        get {
            return this.synopsisField;
        }
        set {
            this.synopsisField = value;
        }
    }
    
    /// <remarks/>
    public string image {
        get {
            return this.imageField;
        }
        set {
            this.imageField = value;
        }
    }

	public Contract_MALAnimeResponse ToContract()
	{
		Contract_MALAnimeResponse contract = new Contract_MALAnimeResponse();

		contract.animeType = type;
		contract.end_date = end_date;
		contract.english = english;
		contract.episodes = episodes;
		contract.id = id;
		contract.image = image;
		contract.score = score;
		contract.start_date = start_date;
		contract.status = status;
		contract.synonyms = synonyms;
		contract.synopsis = synopsis;
		contract.title = title;

		return contract;
	}
}
