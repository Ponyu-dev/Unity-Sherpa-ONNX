#!/usr/bin/env bash
#
# Please download required files from
# https://huggingface.co/desh2608/icefall-asr-librispeech-pruned-transducer-stateless7-streaming-small
#
# Note: epoch-99.pt is a symlink to icefall-asr-librispeech-pruned-transducer-stateless7-streaming-small/exp/pretrained.pt

python ./pruned_transducer_stateless7_streaming/export-onnx.py \
    --tokens ./pruned_transducer_stateless7_streaming/20M-en-2023-02-17/tokens.txt \
    --exp-dir ./pruned_transducer_stateless7_streaming/20M-en-2023-02-17 \
    --use-averaged-model False \
    --epoch 99 \
    --avg 1 \
    --decode-chunk-len 32 \
    --num-encoder-layers "2,2,2,2,2" \
    --feedforward-dims "768,768,768,768,768" \
    --nhead "8,8,8,8,8" \
    --encoder-dims "256,256,256,256,256" \
    --attention-dims "192,192,192,192,192" \
    --encoder-unmasked-dims "192,192,192,192,192" \
    --zipformer-downsampling-factors "1,2,4,8,2" \
    --cnn-module-kernels "31,31,31,31,31" \
    --decoder-dim 512 \
    --joiner-dim 512
