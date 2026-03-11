"""
Tests for mp3_backward_decoder.py

These tests do NOT require an actual MP3 file on disk.  Instead they:
  * Synthesise a minimal in-memory AudioSegment (pure PCM, no codec needed)
  * Patch pydub.AudioSegment.from_mp3 so it returns that synthetic segment
  * Verify that reverse_mp3() produces a correctly reversed WAV

A helper also checks the CLI entry-point (main).
"""

import os
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch, MagicMock

# Make sure the project root is on sys.path regardless of how tests are run.
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

from pydub import AudioSegment  # noqa: E402
from mp3_backward_decoder import main, reverse_mp3  # noqa: E402


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_audio_segment(
    samples: list[int],
    *,
    sample_width: int = 2,
    frame_rate: int = 44100,
    channels: int = 1,
) -> AudioSegment:
    """Build a real pydub AudioSegment from raw 16-bit integer samples."""
    fmt = f"<{len(samples)}h"  # little-endian signed shorts
    raw_data = struct.pack(fmt, *samples)
    return AudioSegment(
        data=raw_data,
        sample_width=sample_width,
        frame_rate=frame_rate,
        channels=channels,
    )


def _read_wav_samples(wav_path: str) -> list[int]:
    """Read all 16-bit signed PCM samples from a WAV file (simple parser)."""
    import wave
    with wave.open(wav_path, "rb") as wf:
        raw = wf.readframes(wf.getnframes())
        n_samples = len(raw) // wf.getsampwidth()
        fmt = f"<{n_samples}h"
        return list(struct.unpack(fmt, raw))


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

class TestReverseMp3(unittest.TestCase):

    def _run_reverse(
        self,
        samples: list[int],
        *,
        channels: int = 1,
        explicit_output: bool = False,
    ) -> tuple[list[int], str]:
        """Helper: patch from_mp3, run reverse_mp3, return samples + output path."""
        audio_in = _make_audio_segment(samples, channels=channels)

        with tempfile.TemporaryDirectory() as tmpdir:
            fake_input = os.path.join(tmpdir, "dummy.mp3")
            # Create a zero-byte placeholder so the file-exists check passes.
            open(fake_input, "wb").close()

            out_path = os.path.join(tmpdir, "out.wav") if explicit_output else None

            with patch("mp3_backward_decoder.AudioSegment.from_mp3", return_value=audio_in):
                result_path = reverse_mp3(fake_input, out_path)

            self.assertTrue(os.path.isfile(result_path), "Output WAV file must exist")
            output_samples = _read_wav_samples(result_path)
            return output_samples, result_path

    # ------------------------------------------------------------------
    # Core reversal logic
    # ------------------------------------------------------------------

    def test_samples_are_reversed(self):
        """The output WAV must contain samples in reversed order."""
        original = [100, 200, 300, 400, 500]
        out_samples, _ = self._run_reverse(original)
        self.assertEqual(out_samples, list(reversed(original)))

    def test_single_sample_unchanged(self):
        """A single-sample input reversed is itself."""
        out_samples, _ = self._run_reverse([42])
        self.assertEqual(out_samples, [42])

    def test_empty_audio_is_handled(self):
        """Zero samples → zero samples (no crash)."""
        out_samples, _ = self._run_reverse([])
        self.assertEqual(out_samples, [])

    def test_double_reverse_is_identity(self):
        """Reversing twice must reproduce the original sample sequence."""
        original = [10, 20, 30, 40, 50, 60]
        audio_in = _make_audio_segment(original)

        with tempfile.TemporaryDirectory() as tmpdir:
            fake_input = os.path.join(tmpdir, "dummy.mp3")
            open(fake_input, "wb").close()

            with patch("mp3_backward_decoder.AudioSegment.from_mp3", return_value=audio_in):
                first_path = reverse_mp3(fake_input)

            # Now reverse the already-reversed WAV
            audio_reversed = AudioSegment.from_wav(first_path)
            fake_input2 = os.path.join(tmpdir, "dummy2.mp3")
            open(fake_input2, "wb").close()

            with patch("mp3_backward_decoder.AudioSegment.from_mp3", return_value=audio_reversed):
                second_path = reverse_mp3(fake_input2)

            # Read the output *inside* the temp-dir context so the file still exists.
            round_trip = _read_wav_samples(second_path)

        self.assertEqual(round_trip, original)

    def test_stereo_reversal(self):
        """Stereo (2-channel) frames must be reversed as whole frames."""
        # 4 stereo frames: each frame is [L, R]
        left  = [10, 20, 30, 40]
        right = [11, 21, 31, 41]
        interleaved = []
        for l, r in zip(left, right):
            interleaved.extend([l, r])

        audio_in = _make_audio_segment(interleaved, channels=2)

        with tempfile.TemporaryDirectory() as tmpdir:
            fake_input = os.path.join(tmpdir, "dummy.mp3")
            open(fake_input, "wb").close()

            with patch("mp3_backward_decoder.AudioSegment.from_mp3", return_value=audio_in):
                result_path = reverse_mp3(fake_input)

            # Read the output *inside* the temp-dir context so the file still exists.
            out_samples = _read_wav_samples(result_path)

        # Expected: frames reversed, channel order within each frame preserved
        expected_frames = list(zip(left, right))[::-1]
        expected = [s for frame in expected_frames for s in frame]
        self.assertEqual(out_samples, expected)

    # ------------------------------------------------------------------
    # Output path behaviour
    # ------------------------------------------------------------------

    def test_default_output_path_naming(self):
        """When no output path is given the result ends with _reversed.wav."""
        original = [1, 2, 3]
        _, result_path = self._run_reverse(original)
        self.assertTrue(result_path.endswith("_reversed.wav"), result_path)

    def test_explicit_output_path_used(self):
        """When an explicit output path is given it must be used."""
        original = [1, 2, 3]
        _, result_path = self._run_reverse(original, explicit_output=True)
        self.assertTrue(result_path.endswith("out.wav"), result_path)

    # ------------------------------------------------------------------
    # Error handling
    # ------------------------------------------------------------------

    def test_missing_input_raises_file_not_found(self):
        """reverse_mp3 must raise FileNotFoundError for a missing source."""
        with self.assertRaises(FileNotFoundError):
            reverse_mp3("/nonexistent/path/audio.mp3")


class TestMain(unittest.TestCase):

    def test_no_args_returns_1(self):
        self.assertEqual(main([]), 1)

    def test_missing_file_returns_2(self):
        rc = main(["/nonexistent/file.mp3"])
        self.assertEqual(rc, 2)

    def test_successful_run_returns_0(self):
        audio_in = _make_audio_segment([10, 20, 30])

        with tempfile.TemporaryDirectory() as tmpdir:
            fake_input = os.path.join(tmpdir, "dummy.mp3")
            open(fake_input, "wb").close()
            fake_output = os.path.join(tmpdir, "out.wav")

            with patch("mp3_backward_decoder.AudioSegment.from_mp3", return_value=audio_in):
                rc = main([fake_input, fake_output])

        self.assertEqual(rc, 0)


if __name__ == "__main__":
    unittest.main()
