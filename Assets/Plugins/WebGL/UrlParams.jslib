mergeInto(LibraryManager.library, {
  GetUrlParam: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    var params = new URLSearchParams(window.location.search);
    var value = params.get(key) || "";
    var bufferSize = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(value, buffer, bufferSize);
    return buffer;
  }
});
