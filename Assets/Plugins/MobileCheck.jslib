mergeInto(LibraryManager.library, {
    // 💡ブラウザの情報を調べて、スマホ（モバイル）なら true、PCなら false を返す魔法の関数
    IsMobileBrowser: function () {
        var userAgent = navigator.userAgent || navigator.vendor || window.opera;
        
        // iPhone, iPad, Android などの文字が含まれているかチェック
        if (/android|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od|ad)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|xo)|pler|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(userAgent)) {
            return true;
        }
        return false;
    }
});
