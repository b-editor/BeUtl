﻿namespace Beutl.Models;

public enum FrameCacheDeletionStrategy
{
    // |■■■■■■■■■■■■□□□■□■□□■■|
    //   ^                  ^
    //   ここから削除する    再生位置
    // - ブロックが断片的になっている部分は描画が遅いので削除しない、
    //   逆にブロックが固まっている部分は描画が早くキャッシュする必要性が少ないので削除する
    // - 再生時に使います
    // - この方法でもキャッシュの上限を超える場合、Farを使って削除します
    BackwardBlock = 1,

    // アクセスされていないキャッシュから順に削除
    // 編集時に使う
    Old = 2,

    // |□■■■■■■■■□□■■□|
    //   ^     ^
    //   |     再生位置
    //   ここから削除する (後方)
    Far = 3
}
