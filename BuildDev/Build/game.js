require('./weapp-adapter');
GameGlobal.WebAssembly = WXWebAssembly;
canvas.id = "";
canvas.style.width = window.innerWidth * window.devicePixelRatio //获取屏幕实际宽度
canvas.style.height = window.innerHeight * window.devicePixelRatio //获取屏幕实际高度
canvas.width =  window.innerWidth  * window.devicePixelRatio //真实的像素宽度
canvas.height = window.innerHeight * window.devicePixelRatio //真实的像素高度
console.log('innerWidth', window.innerWidth, window.innerHeight, window.devicePixelRatio)
 
 console.log("wx.getSystemInfoSync : \n" + JSON.stringify(wx.getSystemInfoSync()))
GameGlobal.Module = {};
GameGlobal.UnityLoader = {
SystemInfo: {

    width:  (function() {
      console.log('width', canvas.width)
      return canvas.width;
    }()),
    height:  (function() {
      console.log('height', canvas.height)
      return canvas.height;
    }()),
    gpu: (function() {
        var gl = canvas.getContext("webgl");
        console.log('gl', gl)
        if(gl) {
          var renderedInfo = gl.getExtension("WEBGL_debug_renderer_info");
          if(renderedInfo) {
            return gl.getParameter(renderedInfo.UNMASKED_RENDERER_WEBGL);
          }
        }
        console.log('unknown gpu')
        return 'unknown';
      })(),
      browser: 'wx',
      browserVersion: '0.0',
      language: window.navigator.userLanguage || window.navigator.language,
      hasWebGL: (function() {     
        // webgl1.0
        return 1;
      })(),   
  }
};

var gameInstance = {
  url: 'urlxxx',
  onProgress: undefined,
  compatibilityCheck: undefined,
  Module: {
    IsWxGame: true,
    preLoaDataPath: 'Build.data.unityweb.bin',//.bin
    wasmPath: 'Build.wasm.br.bin',//wasm_pub_empty_h5.wasm.br.bin // wasm_pub_empty_h5.wasm.code.unityweb.bin
    // wasmBin:"",
    graphicsAPI: ["WebGL 2.0", "WebGL 1.0"],
    onAbort: function(what){
      if (what !== undefined) {
        this.print(what);
        this.printErr(what);
        what = JSON.stringify(what);
      } else {
        what = '';
      }
      throw 'abort(' + what + ') at ' + this.stackTrace();
    },
    preRun: [],
    postRun: [],
    print: function (message) { console.log(message); },
    printErr: function (message) { console.error(message); },
    Jobs: {},
    canvas: canvas,
    buildDownloadProgress: {},

    resolveBuildUrl: function (buildUrl) { return buildUrl.match(/(http|https|ftp|file):\/\//) ? buildUrl : url.substring(0, url.lastIndexOf("/") + 1) + buildUrl; },
    streamingAssetsUrl: function () { return GameGlobal.cdn + "StreamingAssets"  },////resolveURL(this.resolveBuildUrl("../StreamingAssets"))
    // pthreadMainPrefixURL: "Build/",
    webglContextAttributes: 
    {
      premultipliedAlpha: 1,
      preserveDrawingBuffer: 1
    }
  },
  SetFullscreen: function() {
    if (gameInstance.Module.SetFullscreen)
      return gameInstance.Module.SetFullscreen.apply(gameInstance.Module, arguments);
  },
  SendMessage: function() {
    if (gameInstance.Module.SendMessage)
      return gameInstance.Module.SendMessage.apply(gameInstance.Module, arguments);
  },
};

GameGlobal.cdn = "http://10.86.96.108:8080/";

function startUnity(){
  gameInstance.Module.gameInstance = gameInstance;
  gameInstance.popup = function (message, callbacks) { return UnityLoader.Error.popup(gameInstance, message, callbacks); };

  GameGlobal.Module = gameInstance.Module
  var framework = require('./BuildDev.wasm.framework.unityweb.js');
  // const { Module } = require('module');
  framework(GameGlobal.Module)
  var gl = canvas.getContext("webgl");
  gl.scissor(0, 0, canvas.width, canvas.height);
}

var platform = wx.getSystemInfoSync().platform;
if(platform == "devtools") {
  gameInstance.Module["wasmPath"] = 'BuildDev.wasm.code.unityweb.bin';
  gameInstance.Module["preLoaDataPath"] = 'BuildDev.data.unityweb.bin';
  // // gameInstance.Module["wasmBin"]=wx.getFileSystemManager().readFileSync(gameInstance.Module["wasmPath"]);
  // // gameInstance.Module["rawData"]=wx.getFileSystemManager().readFileSync(gameInstance.Module["preLoaDataPath"]);
  startUnity();
} else {
  var dataLoaded=0, codeLoaded=0;

  // wx.request({
  //   url: cdn + gameInstance.Module["wasmPath"],
  //   responseType: 'arraybuffer',
  //   timeout:10000,
  //   success: ({ data }) => {
  //     console.log("data : \n" + data)
  //     codeLoaded =1;
  //     gameInstance.Module["wasmBin"] = data;//decompress(data);
  //     // gameInstance.Module["wasmBin"] = GameGlobal.UnityLoader.Compression.brotli.decompress(data);
  //     console.log("wasm bin loaded  ");
  //     if(dataLoaded){
  //       startUnity();
  //     }
  //   },
  //   fail:function(res){
  //     console.log("xxxxxxxxxxxxxxxxxxxxxxxxxx");
  //     console.log("res.errorMsg: " + res.errMsg);
  //     console.log("xxxxxxxxxxxxxxxxxxxxxxxxxx");
  //   }
  // });
  
  wx.request({
    url: cdn + gameInstance.Module["preLoaDataPath"],
    responseType: 'arraybuffer',
    timeout: 10000,
    success: ({ data }) => {
      dataLoaded =1;
      gameInstance.Module["rawData"] = data;
      console.log("raw Data loaded  ");
      if(codeLoaded){
        startUnity();
      }
    }
  });
  
  // wx.downloadFile({
  //   url: cdn + gameInstance.Module["preLoaDataPath"],
  //   success:(res)=>{
  //     if(res.statusCode == 200){
  //       var path = wx.getFileSystemManager().saveFileSync(res.tempFilePath, wx.env.USER_DATA_PATH+"/"+gameInstance.Module["preLoaDataPath"]);
  //       gameInstance.Module["preLoaDataPath"] = path;
  //       dataLoaded =1;
  //       console.log("dataLoaded:  " + path);
  //       // if(codeLoaded){
  //         startUnity();
  //       // }
  //     }
  //   }
  // });
  
  wx.downloadFile({
    url: cdn + gameInstance.Module["wasmPath"],
    success:(res)=>{
      if(res.statusCode == 200){
  
        var path = wx.getFileSystemManager().saveFileSync(res.tempFilePath, wx.env.USER_DATA_PATH+"/"+gameInstance.Module["wasmPath"]);
        gameInstance.Module["wasmPath"] = path;
        codeLoaded =1;
        console.log("codeLoaded:  " + path);
        if(dataLoaded){
          startUnity();
        }
      }
    }
  });
}
