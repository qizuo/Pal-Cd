package com.unity3d.player; // 替换成你的包名

// 关键：必须导入 UnityPlayerActivity 类
import com.cdjc.gamesdk.core.firebase.FirebaseSdk;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

// 关键：你的类继承了 UnityPlayerActivity
public class OverrideActivity extends UnityPlayerActivity {

  // 重写 onCreate 方法，在 Unity 启动时添加自定义逻辑
  @Override
  protected void onCreate(Bundle savedInstanceState) {
    // 1. 必须调用父类的 onCreate，保证 Unity 正常运行
    super.onCreate(savedInstanceState);
    // 2. 在 Logcat 中打印一条日志，用于验证自定义 Activity 是否生效
    Log.d("OverrideActivity", "自定义 Activity 启动成功！");
    Intent intent = getIntent();
    handleIntent(intent);
  }
  
  static int gameLevel;
  public static void SetGameLevel(int level){
      gameLevel = level;

      if(!isNotifyOpen){
          return;
      }
      Bundle bundle = new Bundle();
      bundle.putInt("user_level",gameLevel);
      bundle.putInt("msg_id",1);
      Log.d("notify", "handleIntent: start game b."+gameLevel);
      FirebaseSdk.logEvent("msg_clicked",bundle);
  }

    static boolean isNotifyOpen = false;
    private void handleIntent(Intent intent) {
        isNotifyOpen = false;
        if (intent != null && intent.getBooleanExtra("isFromNotification", false)) {
            //从通知启动，进行打点记录等操作
            var msgId = intent.getIntExtra("notifyDescIndex",0);
            Log.d("notify", "handleIntent: start game by notification."+msgId);
            //从通知启动，进行打点记录等操作
            isNotifyOpen = true;
        }else{
            //不是从通知启动
            Log.d("notify", "handleIntent: start game by other.");
        }
    }

    public static int GetNotifyId(int index)
    {
        if(index == 0)
        {
            return R.layout.notify_custom_layout;
        }
        if(index == 1){
            return R.drawable.notify_bg;
        }
        if(index == 2){
            return R.string.notify_title;
        }
        if(index == 3){
            return R.string.notify_desc_1;
        }
        if(index == 4){
            return R.string.notify_desc_2;
        }
        if(index == 5){
            return R.string.notify_desc_3;
        }
        if(index == 6){
            return R.string.notify_desc_4;
        }
        if(index == 7){
            return R.string.notify_desc_5;
        }
        if(index == 8){
            return R.string.notify_desc_6;
        }
        if(index == 9){
            return R.string.notify_desc_7;
        }
        if(index == 10){
            return R.string.notify_desc_8;
        }
        if(index == 11){
            return R.string.notify_desc_9;
        }
        if(index == 12){
            return R.string.notify_desc_10;
        }
        if(index == 13){
            return R.string.notify_desc_11;
        }
        return R.string.notify_desc_12;
    }
}