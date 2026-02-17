#!/usr/bin/env bash

export CUDA_VISIBLE_DEVICES=

./zipformer/export-onnx.py \
  --tokens ./icefall-asr-librispeech-zipformer-small-2023-05-16/data/lang_bpe_500/tokens.txt \
  --use-averaged-model 0 \
  --epoch 99 \
  --avg 1 \
  --exp-dir ./icefall-asr-librispeech-zipformer-small-2023-05-16/exp \
  \
  --num-encoder-layers "2,2,2,2,2,2" \
  --downsampling-factor "1,2,4,8,4,2" \
  --feedforward-dim "512,768,768,768,768,768" \
  --num-heads "4,4,4,8,4,4" \
  --encoder-dim "192,256,256,256,256,256" \
  --query-head-dim 32 \
  --value-head-dim 12 \
  --pos-head-dim 4 \
  --pos-dim 48 \
  --encoder-unmasked-dim "192,192,192,192,192,192" \
  --cnn-module-kernel "31,31,15,15,15,31" \
  --decoder-dim 512 \
  --joiner-dim 512 \
  --causal False \
  --chunk-size "16,32,64,-1" \
  --left-context-frames "64,128,256,-1"
