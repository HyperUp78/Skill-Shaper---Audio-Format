using SkillShaper.AutoDj.Models;

namespace SkillShaper.AutoDj.Services;

public sealed class AudioLibraryService
{
    private const double DefaultBpm = 120.0;
    private const string UnknownArtist = "Unknown";

    /// <summary>
    /// Requests the audio storage permission required for the current Android version.
    /// Returns true when permission is granted.
    /// </summary>
    public async Task<bool> EnsureAudioPermissionAsync()
    {
#if ANDROID
        // Android 13+ (API 33) uses READ_MEDIA_AUDIO; earlier versions use READ_EXTERNAL_STORAGE.
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.RequestAsync<ReadMediaAudioPermission>();
            return status == PermissionStatus.Granted;
        }
        else
        {
            var status = await Permissions.RequestAsync<Permissions.StorageRead>();
            return status == PermissionStatus.Granted;
        }
#else
        return await Task.FromResult(true);
#endif
    }

    /// <summary>
    /// Queries the Android MediaStore for all MP3 files and returns them as TrackInfo objects.
    /// </summary>
    public async Task<IEnumerable<TrackInfo>> LoadPhoneMp3LibraryAsync()
    {
#if ANDROID
        return await Task.Run(() =>
        {
            var tracks = new List<TrackInfo>();

            var context = Android.App.Application.Context;
            var resolver = context.ContentResolver;
            if (resolver is null)
            {
                return tracks;
            }

            var uri = Android.Provider.MediaStore.Audio.Media.ExternalContentUri;
            var projection = new[]
            {
                Android.Provider.MediaStore.Audio.AudioColumns.Data,
                Android.Provider.MediaStore.Audio.AudioColumns.Title,
                Android.Provider.MediaStore.Audio.AudioColumns.Artist,
                Android.Provider.MediaStore.Audio.AudioColumns.Duration,
            };

            var selection = $"{Android.Provider.MediaStore.Audio.AudioColumns.MimeType} = 'audio/mpeg'";

            using var cursor = resolver.Query(uri, projection, selection, null, null);
            if (cursor is null)
            {
                return tracks;
            }

            var dataIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.AudioColumns.Data);
            var titleIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.AudioColumns.Title);
            var artistIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.AudioColumns.Artist);
            var durationIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.AudioColumns.Duration);

            while (cursor.MoveToNext())
            {
                var path = dataIdx >= 0 ? cursor.GetString(dataIdx) : null;
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var title = titleIdx >= 0 ? (cursor.GetString(titleIdx) ?? System.IO.Path.GetFileNameWithoutExtension(path)) : System.IO.Path.GetFileNameWithoutExtension(path);
                var artist = artistIdx >= 0 ? (cursor.GetString(artistIdx) ?? UnknownArtist) : UnknownArtist;
                var durationMs = durationIdx >= 0 ? cursor.GetLong(durationIdx) : 0L;

                tracks.Add(new TrackInfo
                {
                    Path = path,
                    Title = title,
                    Artist = artist,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    EstimatedBpm = DefaultBpm,
                });
            }

            return tracks;
        });
#else
        return await Task.FromResult(Enumerable.Empty<TrackInfo>());
#endif
    }
}

#if ANDROID
/// <summary>
/// Custom MAUI permission for android.permission.READ_MEDIA_AUDIO (Android 13+).
/// </summary>
internal sealed class ReadMediaAudioPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { ("android.permission.READ_MEDIA_AUDIO", true) };
}
#endif
