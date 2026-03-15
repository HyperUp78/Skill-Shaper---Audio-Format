"""
MP3 Backward Decoder
====================
Decodes an MP3 file, reverses the PCM sample stream so the audio plays
backwards (sounds weird / reversed), and writes the result to a WAV file.

Usage
-----
    python mp3_backward_decoder.py <input.mp3> [output.wav]

If no output path is given the result is saved as ``<input>_reversed.wav``
next to the source file.
"""

import sys
import os
from pydub import AudioSegment


def reverse_mp3(input_path: str, output_path: str | None = None) -> str:
    """Decode *input_path* (MP3), reverse every PCM sample, save as WAV.

    Parameters
    ----------
    input_path:
        Path to the source MP3 file.
    output_path:
        Destination WAV path.  When *None* the output file is placed in the
        same directory as *input_path* with ``_reversed.wav`` appended to the
        stem.

    Returns
    -------
    str
        Absolute path of the written WAV file.
    """
    if not os.path.isfile(input_path):
        raise FileNotFoundError(f"Input file not found: {input_path!r}")

    # Resolve default output path
    if output_path is None:
        base, _ = os.path.splitext(input_path)
        output_path = base + "_reversed.wav"

    # ------------------------------------------------------------------
    # 1. Decode MP3 → raw PCM samples via pydub / ffmpeg
    # ------------------------------------------------------------------
    audio: AudioSegment = AudioSegment.from_mp3(input_path)

    # ------------------------------------------------------------------
    # 2. Reverse the byte-level waveform
    #
    # pydub stores raw PCM as a bytes object.  Each sample occupies
    # (sample_width) bytes.  To correctly reverse the audio we must
    # reverse on *sample* boundaries (not individual bytes), otherwise
    # multi-byte samples would also have their byte order flipped.
    # ------------------------------------------------------------------
    sample_width: int = audio.sample_width          # bytes per sample (e.g. 2 for 16-bit)
    channels: int = audio.channels
    frame_width: int = sample_width * channels      # bytes per multi-channel frame

    raw: bytes = audio.raw_data

    # Slice raw bytes into frames and reverse the frame order.
    frame_count = len(raw) // frame_width
    frames = [
        raw[i * frame_width: (i + 1) * frame_width]
        for i in range(frame_count)
    ]
    frames.reverse()
    reversed_raw = b"".join(frames)

    # ------------------------------------------------------------------
    # 3. Rebuild AudioSegment with the reversed raw data and export
    # ------------------------------------------------------------------
    reversed_audio = audio._spawn(reversed_raw)
    reversed_audio.export(output_path, format="wav")

    return os.path.abspath(output_path)


def main(argv: list[str] | None = None) -> int:
    """Command-line entry point."""
    if argv is None:
        argv = sys.argv[1:]

    if not argv:
        print(
            "Usage: python mp3_backward_decoder.py <input.mp3> [output.wav]",
            file=sys.stderr,
        )
        return 1

    input_path = argv[0]
    output_path = argv[1] if len(argv) >= 2 else None

    try:
        result = reverse_mp3(input_path, output_path)
        print(f"Reversed audio written to: {result}")
        return 0
    except FileNotFoundError as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 2
    except Exception as exc:  # noqa: BLE001
        print(f"Unexpected error: {exc}", file=sys.stderr)
        return 3


if __name__ == "__main__":
    sys.exit(main())
