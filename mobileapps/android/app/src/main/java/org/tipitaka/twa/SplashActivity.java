package org.tipitaka.twa;

import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import androidx.appcompat.app.AppCompatActivity;
import com.google.androidbrowserhelper.trusted.TwaLauncher;

public class SplashActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_splash);

        // Display the custom splash design (Image 3) for 3 seconds
        new Handler().postDelayed(() -> {
            launchTwa();
        }, 3000);
    }

    private void launchTwa() {
        // Use TwaLauncher to open the website directly from the splash screen
        TwaLauncher launcher = new TwaLauncher(this);
        launcher.launch(Uri.parse("https://tipitaka.org"));
        
        // Close the splash activity after launching the website
        new Handler().postDelayed(this::finish, 1000);
    }
}