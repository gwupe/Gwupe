/*=============================================================================|
| Project : Bauglir Internet Library                                           |
|==============================================================================|
| Content: Generic connection and server                                       |
|==============================================================================|
| Copyright (c)2011-2012, Bronislav Klucka                                     |
| All rights reserved.                                                         |
| Source code is licenced under original 4-clause BSD licence:                 |
| http://licence.bauglir.com/bsd4.php                                          |
|                                                                              |
|                                                                              |
| Project download homepage:                                                   |
|   http://code.google.com/p/bauglir-websocket/                                |
| Project homepage:                                                            |
|   http://www.webnt.eu/index.php                                              |
| WebSocket RFC:                                                               |
|   http://tools.ietf.org/html/rfc6455                                         |
|                                                                              |
|                                                                              |
|=============================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Bauglir.Ex

{
  public class WebSocketIndexer
  {
    private static int fIndex = 0;
    
    public static int GetIndex()
    {
      return fIndex++;
    }
  }

  /// <summary>
  /// list of supported frames, see websocket specification for documentation
  /// </summary>
  public static class WebSocketFrame
  {
    public const int Continuation = 0x00;
    public const int Text = 0x01;
    public const int Binary = 0x02;
    public const int Close = 0x08;
    public const int Ping = 0x09;
    public const int Pong = 0x0A;
  }


  /// <summary>
  /// list of close reason codes, see websocket specification for documentation
  /// </summary>
  public static class WebSocketCloseCode
  {
    public const int Normal = 1000;
    public const int Shutdown = 1001;
    public const int ProtocolError = 1002;
    public const int DataError = 1003;
    public const int Reserved1 = 1004;
    public const int NoStatus = 1005;
    public const int CloseError = 1006;
    public const int UTF8Error = 1007;
    public const int PolicyError = 1008;
    public const int TooLargeMessage = 1009;
    public const int ClientExtensionError = 1010;
    public const int ServerRequestError = 1011;
    public const int TLSError = 1015;



    /*
     * I probably do not need this...
    public static void GetReason(int aCode, string aData = "")
    {
      string result = String.Empty;
      switch (aCode)
      {
        case WebSocketCloseCode.Normal: result = ""; break;
        case WebSocketCloseCode.Shutdown: result = ""; break;
        case WebSocketCloseCode.ProtocolError: result = ""; break;
        case WebSocketCloseCode.DataError: result = "Protocol error"; break;
      }
      if (aData != String.Empty) result += (result == String.Empty ? String.Empty : ": ") + aData;
    }
     */ 
  }


 
  

  public class WebSocketHeaders : DictionaryBase  
  {
    public String this[ String key ]  
    {
      get  { return( (String) Dictionary[key] ); }
      set  { Dictionary[key] = value; }
    }

    public ICollection Keys  
    {
      get  { return( Dictionary.Keys ); }
    }

    public ICollection Values  
    {
      get  { return( Dictionary.Values ); }
    }

    public void Add( String key, String value )  
    {
      Dictionary.Add( key, value );
    }
    
    public void Append( String key, String value )  
    {
      if (this.Contains(key)) this[key] += ',' + value;
      else this[key] = value;
    }

    public bool Contains( String key )  
    {
      return( Dictionary.Contains( key ) );
    }

    public void Remove( String key )  
    {
      Dictionary.Remove( key );
    }
    
    public string ToHeaders()
    {
      string result = String.Empty;
      foreach( DictionaryEntry entry in this )
      {
        result += entry.Key + ": " + entry.Value + "\r\n";
      }
      if (result != String.Empty)
        result += "\r\n";
      return result;
    }

    protected override void OnInsert( Object key, Object value )  
    {
      if ( key.GetType() != typeof(System.String) )
        throw new ArgumentException( "key must be of type String.", "key" );
      if ( value.GetType() != typeof(System.String) )
        throw new ArgumentException( "value must be of type String.", "value" );
    }

    protected override void OnRemove( Object key, Object value )  
    {
      if ( key.GetType() != typeof(System.String) )
        throw new ArgumentException( "key must be of type String.", "key" );
    }

    protected override void OnSet( Object key, Object oldValue, Object newValue )  
    {
      if ( key.GetType() != typeof(System.String) )
        throw new ArgumentException( "key must be of type String.", "key" );

      if ( newValue.GetType() != typeof(System.String) )
         throw new ArgumentException( "newValue must be of type String.", "newValue" );
    }

    protected override void OnValidate( Object key, Object value )  
    {
      if ( key.GetType() != typeof(System.String) )
        throw new ArgumentException( "key must be of type String.", "key" );

      if ( value.GetType() != typeof(System.String) )
        throw new ArgumentException( "value must be of type String.", "value" );
    }

  }  
    
}
