#import <AVFoundation/AVFoundation.h>

extern "C"
{
    void _SherpaOnnx_ConfigureAudioSessionPlayAndRecord()
    {
        AVAudioSession *session = [AVAudioSession sharedInstance];
        NSError *error = nil;

        [session setCategory:AVAudioSessionCategoryPlayAndRecord
                 withOptions:AVAudioSessionCategoryOptionDefaultToSpeaker |
                             AVAudioSessionCategoryOptionAllowBluetooth
                       error:&error];

        if (error)
        {
            NSLog(@"[SherpaOnnx] AudioSession setCategory error: %@", error.localizedDescription);
            return;
        }

        [session setActive:YES error:&error];

        if (error)
        {
            NSLog(@"[SherpaOnnx] AudioSession setActive error: %@", error.localizedDescription);
            return;
        }

        NSLog(@"[SherpaOnnx] AudioSession configured: PlayAndRecord + DefaultToSpeaker");
    }
}
