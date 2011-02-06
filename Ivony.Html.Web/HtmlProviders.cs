﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivony.Html.Parser;
using System.Web.Hosting;
using System.Web;
using System.IO;
using System.Web.Caching;
using System.Globalization;

namespace Ivony.Html.Web
{

  /// <summary>
  /// 提供Jumony Web所有提供程序的管理和注册的静态类
  /// </summary>
  public static class HtmlProviders
  {

    static HtmlProviders()
    {
      ParserProviders = new SynchronizedCollection<IHtmlParserProvider>( _parserProvidersSync );
      ContentProviders = new SynchronizedCollection<IHtmlContentProvider>( _contentProvidersSync );
      RequestMappers = new SynchronizedCollection<IRequestMapper>( _mappersSync );
      CachePolicyProviders = new SynchronizedCollection<IHtmlCachePolicyProvider>( _cachePoliciesSync );


      ContentProviders.Add( new StaticFileLoader() );
      ContentProviders.Add( new AspxFileLoader() );

      RequestMappers.Add( new DefaultRequestMapper() );
    }


    private static readonly object _parserProvidersSync = new object();

    /// <summary>
    /// 所有解析器提供程序
    /// </summary>
    public static ICollection<IHtmlParserProvider> ParserProviders
    {
      get;
      private set;
    }


    private static readonly object _contentProvidersSync = new object();

    /// <summary>
    /// 所有内容提供程序
    /// </summary>
    public static ICollection<IHtmlContentProvider> ContentProviders
    {
      get;
      private set;
    }


    private static readonly object _mappersSync = new object();

    /// <summary>
    /// 所有请求映射提供程序
    /// </summary>
    public static ICollection<IRequestMapper> RequestMappers
    {
      get;
      private set;
    }


    private static readonly object _cachePoliciesSync = new object();

    /// <summary>
    /// 所有缓存策略提供程序
    /// </summary>
    public static ICollection<IHtmlCachePolicyProvider> CachePolicyProviders
    {
      get;
      private set;
    }


    /// <summary>
    /// 映射请求
    /// </summary>
    /// <param name="request">当前 HTTP 请求信息</param>
    /// <returns>请求映射信息</returns>
    public static RequestMapResult MapRequest( HttpRequest request )
    {

      if ( request == null )
        throw new ArgumentNullException( "request" );



      lock ( _mappersSync )
      {
        foreach ( var mapper in RequestMappers )
        {
          var result = mapper.MapRequest( request );
          if ( result != null )
          {
            result.Mapper = mapper;
            return result;
          }
        }
      }


      return null;
    }



    /// <summary>
    /// 加载 HTML 文档内容
    /// </summary>
    /// <param name="virtualPath">文档的虚拟路径</param>
    /// <returns>HTML 内容加载结果</returns>
    public static HtmlContentResult LoadContent( HttpContextBase context, string virtualPath )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( virtualPath == null )
        throw new ArgumentNullException( "virtualPath" );


      if ( !VirtualPathUtility.IsAppRelative( virtualPath ) )
        throw new ArgumentException( "virtualPath只能使用应用程序根相对路径", "virtualPath" );


      lock ( _contentProvidersSync )
      {
        foreach ( var provider in ContentProviders )
        {
          var result = provider.LoadContent( context, virtualPath );

          if ( result != null )
            return result;
        }
      }


      return null;
    }

    /// <summary>
    /// 加载 HTML 文档
    /// </summary>
    /// <param name="context">当前请求的 HttpContext 对象</param>
    /// <param name="virtualPath">文档的虚拟路径</param>
    /// <returns>HTML 文档对象</returns>
    public static IHtmlDocument LoadDocument( HttpContextBase context, string virtualPath )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( virtualPath == null )
        throw new ArgumentNullException( "virtualPath" );

      if ( !VirtualPathUtility.IsAppRelative( virtualPath ) )
        throw new ArgumentException( "virtualPath只能使用应用程序根相对路径，即以~/开头的路径，调用VirtualPathUtility.ToAppRelative方法或使用HttpRequest.AppRelativeCurrentExecutionFilePath属性获取", "virtualPath" );


      var content = LoadContent( context, virtualPath );
      if ( content == null )
        return null;

      return ParseDocument( context, content );
    }


    /// <summary>
    /// 加载一个 Web 页面
    /// </summary>
    /// <param name="context">当前请求的 HttpContext 对象</param>
    /// <param name="virtualPath">页面的虚拟路径</param>
    /// <returns>Web 页面对象</returns>
    public static WebPage LoadPage( HttpContextBase context, string virtualPath )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( virtualPath == null )
        throw new ArgumentNullException( "virtualPath" );

      if ( !VirtualPathUtility.IsAppRelative( virtualPath ) )
        throw new ArgumentException( "virtualPath只能使用应用程序根相对路径，即以~/开头的路径，调用VirtualPathUtility.ToAppRelative方法或使用HttpRequest.AppRelativeCurrentExecutionFilePath属性获取", "virtualPath" );


      var content = LoadContent( context, virtualPath );
      if ( content == null )
        return null;

      return LoadPage( context, content );

    }



    /// <summary>
    /// 加载一个 Web 页面
    /// </summary>
    /// <param name="context">当前请求的 HttpContext 对象</param>
    /// <param name="content">要被加载的 HTML 内容</param>
    /// <returns>Web 页面对象</returns>
    public static WebPage LoadPage( HttpContextBase context, HtmlContentResult content )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( content == null )
        throw new ArgumentNullException( "content" );

      var document = ParseDocument( context, content );

      return new WebPage( document, content.ContentUri, content.CacheKey );
    }

    /// <summary>
    /// 获取用于分析 HTML 文档的分析器
    /// </summary>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <param name="htmlContent">HTML 文档内容</param>
    /// <returns>HTML 分析器相关信息</returns>
    public static HtmlParserResult GetParser( HttpContextBase context, Uri contentUri, string htmlContent )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( contentUri != null && !contentUri.IsAbsoluteUri )
        throw new ArgumentException( "contentUri只能为null或绝对URI", "contentUri" );




      lock ( _parserProvidersSync )
      {
        foreach ( var provider in ParserProviders )
        {
          var result = provider.GetParser( context, contentUri, htmlContent );

          if ( result != null )
          {
            result.Provider = provider;
            return result;
          }
        }
      }


      //默认行为
      return new HtmlParserResult()
      {
        Parser = new JumonyHtmlParser(),
        DomProvider = new DomProvider(),
      };
    }



    private const string DocumentCacheKey = "HtmlProviders_HtmlDocumentCache_{0}";

    /// <summary>
    /// 分析 HTML 文档，此方法会根据情况缓存文档模型
    /// </summary>
    /// <param name="context">当前请求的 HttpContext 对象</param>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <param name="result">文档加载结果</param>
    /// <returns>HTML 文档对象</returns>
    public static IHtmlDocument ParseDocument( HttpContextBase context, HtmlContentResult contentResult )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( contentResult == null )
        throw new ArgumentNullException( "contentResult" );



      var content = contentResult.Content;

      var result = GetParser( context, contentResult.ContentUri, content );


      if ( contentResult.CacheKey != null && result.DomProvider != null )//如果可以缓存
      {
        var key = contentResult.CacheKey;
        var cacheKey = string.Format( CultureInfo.InvariantCulture, DocumentCacheKey, key );

        var createDocument = Cache.Get( key ) as Func<IHtmlDomProvider, IHtmlDocument>;

        if ( createDocument != null )
          return createDocument( result.DomProvider );



        var document = ParseDocument( result, content );

        createDocument = document.Compile();

        Cache.Insert( cacheKey, createDocument, new CacheDependency( new string[0], new[] { key } ) );

        return document;
      }

      else

        return ParseDocument( result, content );
    }


    /// <summary>
    /// 分析 HTML 文档，此方法永不缓存
    /// </summary>
    /// <param name="context">当前请求的 HttpContext 对象</param>
    /// <param name="virtualPath">请求的虚拟路径</param>
    /// <param name="htmlContent">文档内容</param>
    /// <returns>HTML 文档对象</returns>
    public static IHtmlDocument ParseDocument( HttpContextBase context, Uri contentUri, string htmlContent )
    {

      if ( context == null )
        throw new ArgumentNullException( "context" );

      if ( htmlContent == null )
        throw new ArgumentNullException( "htmlContent" );

      if ( contentUri != null && !contentUri.IsAbsoluteUri )
        throw new ArgumentException( "contentUri只能为null或绝对URI", "contentUri" );


      var result = GetParser( context, contentUri, htmlContent );

      return ParseDocument( result, htmlContent );
    }


    private static IHtmlDocument ParseDocument( HtmlParserResult result, string htmlContent )
    {
      var parser = result.Parser;

      var document = parser.Parse( htmlContent );

      if ( result.Provider != null )
        result.Provider.ReleaseParser( parser );

      return document;
    }



    private static Cache Cache
    {
      get { return HostingEnvironment.Cache; }
    }




    /// <summary>
    /// 获取缓存键（依据）
    /// </summary>
    /// <param name="context">当前HTTP请求信息</param>
    /// <returns>缓存键，对于可能产生同一结果的请求，应产生同一缓存键</returns>
    public static string GetCacheKey( HttpContextBase context )
    {
      lock ( _cachePoliciesSync )
      {
        foreach ( var policy in CachePolicyProviders )
        {
          string cacheKey = policy.GetCacheKey( context );
          if ( cacheKey != null )
            return cacheKey;
        }
      }

      return DefaultCachePolicy.GetCacheKey( context );

    }

    /// <summary>
    /// 获取缓存策略
    /// </summary>
    /// <param name="context">当前HTTP请求信息</param>
    /// <param name="handler">当前负责处理请求的处理程序</param>
    /// <param name="page">处理后的文档</param>
    /// <returns>缓存策略</returns>
    /// <remarks>缓存策略决定了缓存时间和缓存依赖项</remarks>
    public static HtmlCachePolicy GetCachePolicy( HttpContextBase context, IHtmlHandler handler, RawResponse cacheItem )
    {
      lock ( _cachePoliciesSync )
      {
        foreach ( var provider in CachePolicyProviders )
        {
          var policy = provider.GetPolicy( context, handler, cacheItem );
          if ( policy != null )
            return policy;
        }
      }

      return DefaultCachePolicy.GetPolicy( context, handler, cacheItem );
    }







  }

}
