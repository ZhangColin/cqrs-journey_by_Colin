﻿using System;
using System.ComponentModel.DataAnnotations;
using Infrastructure.Utils;

namespace Conference {
    /// <summary>
    /// 座位类型
    /// </summary>
    public class SeatType {
        public SeatType() {
            this.Id = GuidUtil.NewSequentialId();
        }

        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(70, MinimumLength = 2)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(250)]
        public string Description { get; set; }

        [Range(0, 100000)]
        public int Quantity { get; set; }

        [Range(0, 50000)]
        public decimal Price { get; set; }
    }
}