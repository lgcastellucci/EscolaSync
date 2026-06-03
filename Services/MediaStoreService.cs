using Android.Content;
using Android.OS;
using Android.Provider;
using EscolaSync.Models;
using Uri = Android.Net.Uri;

namespace EscolaSync.Services;

public class MediaStoreService
{
    // MediaStore.MediaColumns contém Id, DisplayName, Data, MimeType, Size
    // MediaStore.Images.ImageColumns (interface) contém BucketDisplayName, DateTaken
    private const string ColId          = "_id";
    private const string ColDisplayName = "display_name";
    private const string ColData        = "_data";
    private const string ColMimeType    = "mime_type";
    private const string ColSize        = "_size";
    private const string ColBucket      = "bucket_display_name";
    private const string ColDateTaken   = "datetaken";

    public List<string> GetAlbums()
    {
        var albums = new List<string>();
        var context = Android.App.Application.Context;
        var uri = MediaStore.Images.Media.ExternalContentUri!;

        using var cursor = context.ContentResolver!.Query(
            uri, new[] { ColBucket }, null, null, $"{ColBucket} ASC");

        if (cursor == null) return albums;

        var seen = new HashSet<string>();
        while (cursor.MoveToNext())
        {
            var name = cursor.GetString(0) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(name) && seen.Add(name))
                albums.Add(name);
        }

        return albums;
    }

    public List<PhotoItem> GetPhotosFromAlbum(string albumName)
    {
        var photos = new List<PhotoItem>();
        var context = Android.App.Application.Context;
        var uri = MediaStore.Images.Media.ExternalContentUri!;

        string[] projection = new[]
        {
            ColId, ColDisplayName, ColData, ColMimeType, ColSize, ColDateTaken
        };

        using var cursor = context.ContentResolver!.Query(
            uri, projection,
            $"{ColBucket} = ?", new[] { albumName },
            $"{ColDateTaken} DESC");

        if (cursor == null) return photos;

        int idCol   = cursor.GetColumnIndexOrThrow(ColId);
        int nameCol = cursor.GetColumnIndexOrThrow(ColDisplayName);
        int pathCol = cursor.GetColumnIndexOrThrow(ColData);
        int mimeCol = cursor.GetColumnIndexOrThrow(ColMimeType);
        int sizeCol = cursor.GetColumnIndexOrThrow(ColSize);
        int dateCol = cursor.GetColumnIndexOrThrow(ColDateTaken);

        while (cursor.MoveToNext())
        {
            photos.Add(new PhotoItem
            {
                Id          = cursor.GetLong(idCol),
                DisplayName = cursor.GetString(nameCol) ?? "foto.jpg",
                FilePath    = cursor.GetString(pathCol) ?? string.Empty,
                MimeType    = cursor.GetString(mimeCol) ?? "image/jpeg",
                SizeBytes   = cursor.GetLong(sizeCol),
                DateTaken   = DateTimeOffset
                                .FromUnixTimeMilliseconds(cursor.GetLong(dateCol))
                                .LocalDateTime
            });
        }

        return photos;
    }

    public async Task<bool> DeletePhotoAsync(PhotoItem photo, Android.App.Activity activity)
    {
        try
        {
            var context = Android.App.Application.Context;
            var photoUri = ContentUris.WithAppendedId(
                MediaStore.Images.Media.ExternalContentUri!, photo.Id);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416
                var pendingIntent = MediaStore.CreateDeleteRequest(
                    context.ContentResolver!, new List<Uri> { photoUri });
#pragma warning restore CA1416

                var tcs = new TaskCompletionSource<bool>();
                DeleteResultCallback = rc => tcs.TrySetResult(rc == (int)Android.App.Result.Ok);

                activity.StartIntentSenderForResult(
                    pendingIntent.IntentSender, 42, null, 0, 0, 0);

                return await tcs.Task;
            }
            else
            {
                return context.ContentResolver!.Delete(photoUri, null, null) > 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MEDIA] Erro ao deletar {photo.DisplayName}: {ex.Message}");
            return false;
        }
    }

    public static Action<int>? DeleteResultCallback;
}
