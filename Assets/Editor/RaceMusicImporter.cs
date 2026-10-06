using UnityEditor;
using UnityEngine;

/// <summary>
/// Музыка из Assets/Resources/RaceMusic: длинные треки грузим потоком (Streaming),
/// иначе каждый трек целиком распаковывается в память при старте игры.
/// </summary>
public class RaceMusicImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/RaceMusic/")) return;
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = true;
    }
}
