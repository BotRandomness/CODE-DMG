class CPU {
    public byte A, B, C, D, E, H, L, F;
    public ushort PC, SP;
    public bool zero, negative, halfCarry, carry;
    public bool IME;
    private bool halted;

    private MMU mmu;

    public CPU(MMU mmu) {
        A = B = C = D = E = H = L = F = 0;
        PC = 0x0000;
        SP = 0x0000; //Boot Rom will set this to 0xFFFE
        zero = negative = halfCarry = carry = false;
        IME = false;

        this.mmu = mmu;

        Console.WriteLine("CPU init");
    }

    public void Reset() {
        A = 0x01;
        F = 0xB0; //Z=1,N=0,H=1,C=1
        UpdateFlagsFromF();
        B = 0x00;
        C = 0x13;
        D = 0x00;
        E = 0xD8;
        H = 0x01;
        L = 0x4D;
        PC = 0x100;
        SP = 0xFFFE;

        mmu.JOYP = 0xCF;
        mmu.DIV = 0x18;
        mmu.IF = 0xE1;
        mmu.LCDC = 0x91;
        mmu.STAT = 0x85;
        mmu.SCY = 0x00;
        mmu.SCX = 0x00;
        mmu.LY = 0x00;
        mmu.LYC = 0x00;
        mmu.BGP = 0xFC;
        mmu.Write(0xFF50, A);
    }

    public int HandleInterrupts() {
        byte interruptFlag = mmu.Read(0xFF0F);
        byte interruptEnable = mmu.Read(0xFFFF);
        byte interrupts = (byte)(interruptFlag & interruptEnable);

        if (interrupts != 0) {
            halted = false;
            if (IME) {
                IME = false;

                for (int bit = 0; bit < 5; bit++) {
                    if ((interrupts & (1 << bit)) != 0) {
                        mmu.Write(0xFF0F, (byte)(interruptFlag & ~(1 << bit)));

                        SP--;
                        mmu.Write(SP, (byte)((PC >> 8) & 0xFF));
                        SP--;
                        mmu.Write(SP, (byte)(PC & 0xFF));

                        PC = GetInterruptHandlerAddress(bit);
                        return 20;
                    }
                }
            }
            return 0;
        }

        if (halted && interrupts != 0)
        {
            halted = false;
        }

        return 0;
    }

    private ushort GetInterruptHandlerAddress(int bit) {
        switch (bit) {
            case 0: return 0x40; //VBlank interrupt handler
            case 1: return 0x48; //LCD STAT interrupt handler
            case 2: return 0x50; //Timer interrupt handler
            case 3: return 0x58; //Serial interrupt handler
            case 4: return 0x60; //Joypad interrupt handler
            default: return 0;
        }
    }    

    private void UpdateFFromFlags() {
        F = 0;
        if (zero) F |= 0x80; //Set the Zero flag (bit 7)
        if (negative) F |= 0x40; //Set the Negative (Subtraction) flag (bit 6)
        if (halfCarry) F |= 0x20; //Set the Half Carry flag (bit 5)
        if (carry) F |= 0x10; //Set the Carry flag (bit 4)
    }

    public void UpdateFlagsFromF() {
        zero = (F & 0x80) != 0;
        negative = (F & 0x40) != 0;
        halfCarry = (F & 0x20) != 0;
        carry = (F & 0x10) != 0;
    }
    
    private ushort Get16BitReg(string pair) {
        switch (pair.ToLower()) {
            case "bc":
                return (ushort)((B << 8) | C);
            case "de":
                return (ushort)((D << 8) | E);
            case "hl":
                return (ushort)((H << 8) | L);
            case "af":
                return (ushort)((A << 8) | F);
            default:
                return 0;
        }
    }

    private void Load16BitReg(string pair, ushort value) {
        switch (pair.ToLower()) {
            case "bc":
                B = (byte)(value >> 8);
                C = (byte)(value & 0xFF);
                break;
            case "de":
                D = (byte)(value >> 8);
                E = (byte)(value & 0xFF);
                break;
            case "hl":
                H = (byte)(value >> 8);
                L = (byte)(value & 0xFF);
                break;
            case "af":
                A = (byte)(value >> 8);
                F = (byte)(value & 0xFF);
                break;
            default:
                break;
        }
    }

    public void Log() {
        UpdateFFromFlags();
        ushort op1 = (PC);
        ushort op2 = (ushort)(PC + 1);
        ushort op3 = (ushort)(PC + 2);
        ushort op4 = (ushort)(PC + 3);
        Console.WriteLine("A: " + A.ToString("X2") + " F: " + F.ToString("X2") + " B: " + B.ToString("X2") + " C: " + C.ToString("X2") + " D: " + D.ToString("X2") + " E: " + E.ToString("X2") + " H: " + H.ToString("X2") + " L: " + L.ToString("X2") + " SP: " + SP.ToString("X4") + " PC: " + "00:" + PC.ToString("X4") + " (" + mmu.Read(op1).ToString("X2") + " " + mmu.Read(op2).ToString("X2") + " " + mmu.Read(op3).ToString("X2") + " " + mmu.Read(op4).ToString("X2") + ")");
    }

    private byte Fetch() {
        return mmu.Read(PC++);
    }

    public int ExecuteInstruction() {
        //Log();
        //if(PC>= 0x100) {Log();}
        
        int interruptCycles = HandleInterrupts();
        if (interruptCycles > 0) {
            return interruptCycles;
        }

        if (halted) {
            return 4;
        }

        byte opcode = Fetch();

        switch (opcode) {
            case 0x00:
                return NOP();
            case 0x01:
                return LD_RR_U16(ref B, ref C);
            case 0x02:
                return LD_ARR_R(ref A, "bc");
            case 0x03:
                return INC_RR("bc");
            case 0x04:
                return INC_R(ref B);
            case 0x05:
                return DEC_R(ref B);
            case 0x06:
                return LD_R_U8(ref B);
            case 0x07:
                return RLCA();
            case 0x08:
                return LD_AU16_SP();
            case 0x09:
                return ADD_HL_RR("bc");
            case 0x0A:
                return LD_R_ARR(ref A, "bc");
            case 0x0B:
                return DEC_RR("bc");
            case 0x0C:
                return INC_R(ref C);
            case 0x0D:
                return DEC_R(ref C);
            case 0x0E:
                return LD_R_U8(ref C);
            case 0x0F:
                return RRCA();
            case 0x10:
                return STOP();
            case 0x11:
                return LD_RR_U16(ref D, ref E);
            case 0x12:
                return LD_ARR_R(ref A, "de");
            case 0x13:
                return INC_RR("de");
            case 0x14:
                return INC_R(ref D);
            case 0x15:
                return DEC_R(ref D);
            case 0x16:
                return LD_R_U8(ref D);
            case 0x17:
                return RLA();
            case 0x18:
                return JR_CON_I8(true);
            case 0x19:
                return ADD_HL_RR("de");
            case 0x1A:
                return LD_R_ARR(ref A, "de");
            case 0x1B:
                return DEC_RR("de");
            case 0x1C:
                return INC_R(ref E);
            case 0x1D:
                return DEC_R(ref E);
            case 0x1E:
                return LD_R_U8(ref E);
            case 0x1F:
                return RRA();
            case 0x20:
                return JR_CON_I8(!zero);
            case 0x21:
                return LD_RR_U16(ref H, ref L);
            case 0x22:
                return LD_AHLI_A();
            case 0x23:
                return INC_RR("hl");
            case 0x24:
                return INC_R(ref H);
            case 0x25:
                return DEC_R(ref H);
            case 0x26:
                return LD_R_U8(ref H);
            case 0x27:
                return DAA();
            case 0x28:
                return JR_CON_I8(zero);
            case 0x29:
                return ADD_HL_RR("hl");
            case 0x2A:
                return LD_A_AHLI();
            case 0x2B:
                return DEC_RR("hl");
            case 0x2C:
                return INC_R(ref L);
            case 0x2D:
                return DEC_R(ref L);
            case 0x2E:
                return LD_R_U8(ref L);
            case 0x2F:
                return CPL();
            case 0x30:
                return JR_CON_I8(!carry);
            case 0x31:
                return LD_SP_U16();
            case 0x32:
                return LD_AHLM_A();
            case 0x33:
                return INC_SP();
            case 0x34:
                return INC_AHL();
            case 0x35:
                return DEC_AHL();
            case 0x36:
                return LD_AHL_U8();
            case 0x37:
                return SCF();
            case 0x38:
                return JR_CON_I8(carry);
            case 0x39:
                return ADD_HL_SP();
            case 0x3A:
                return LD_A_AHLM();
            case 0x3B:
                return DEC_SP();
            case 0x3C:
                return INC_R(ref A);
            case 0x3D:
                return DEC_R(ref A);
            case 0x3E:
                return LD_R_U8(ref A);
            case 0x3F:
                return CCF();
            case 0x40:
                return LD_R1_R2(ref B, ref B);
            case 0x41:
                return LD_R1_R2(ref B, ref C);
            case 0x42:
                return LD_R1_R2(ref B, ref D);
            case 0x43:
                return LD_R1_R2(ref B, ref E);
            case 0x44:
                return LD_R1_R2(ref B, ref H);
            case 0x45:
                return LD_R1_R2(ref B, ref L);
            case 0x46:
                return LD_R_ARR(ref B, "hl");
            case 0x47:
                return LD_R1_R2(ref B, ref A);
            case 0x48:
                return LD_R1_R2(ref C, ref B);
            case 0x49:
                return LD_R1_R2(ref C, ref C);
            case 0x4A:
                return LD_R1_R2(ref C, ref D);
            case 0x4B:
                return LD_R1_R2(ref C, ref E);
            case 0x4C:
                return LD_R1_R2(ref C, ref H);
            case 0x4D:
                return LD_R1_R2(ref C, ref L);
            case 0x4E:
                return LD_R_ARR(ref C, "hl");
            case 0x4F:
                return LD_R1_R2(ref C, ref A);
            case 0x50:
                return LD_R1_R2(ref D, ref B);
            case 0x51:
                return LD_R1_R2(ref D, ref C);
            case 0x52:
                return LD_R1_R2(ref D, ref D);
            case 0x53:
                return LD_R1_R2(ref D, ref E);
            case 0x54:
                return LD_R1_R2(ref D, ref H);
            case 0x55:
                return LD_R1_R2(ref D, ref L);
            case 0x56:
                return LD_R_ARR(ref D, "hl");
            case 0x57:
                return LD_R1_R2(ref D, ref A);
            case 0x58:
                return LD_R1_R2(ref E, ref B);
            case 0x59:
                return LD_R1_R2(ref E, ref C);
            case 0x5A:
                return LD_R1_R2(ref E, ref D);
            case 0x5B:
                return LD_R1_R2(ref E, ref E);
            case 0x5C:
                return LD_R1_R2(ref E, ref H);
            case 0x5D:
                return LD_R1_R2(ref E, ref L);
            case 0x5E:
                return LD_R_ARR(ref E, "hl");
            case 0x5F:
                return LD_R1_R2(ref E, ref A);
            case 0x60:
                return LD_R1_R2(ref H, ref B);
            case 0x61:
                return LD_R1_R2(ref H, ref C);
            case 0x62:
                return LD_R1_R2(ref H, ref D);
            case 0x63:
                return LD_R1_R2(ref H, ref E);
            case 0x64:
                return LD_R1_R2(ref H, ref H);
            case 0x65:
                return LD_R1_R2(ref H, ref L);
            case 0x66:
                return LD_R_ARR(ref H, "hl");
            case 0x67:
                return LD_R1_R2(ref H, ref A);
            case 0x68:
                return LD_R1_R2(ref L, ref B);
            case 0x69:
                return LD_R1_R2(ref L, ref C);
            case 0x6A:
                return LD_R1_R2(ref L, ref D);
            case 0x6B:
                return LD_R1_R2(ref L, ref E);
            case 0x6C:
                return LD_R1_R2(ref L, ref H);
            case 0x6D:
                return LD_R1_R2(ref L, ref L);
            case 0x6E:
                return LD_R_ARR(ref L, "hl");
            case 0x6F:
                return LD_R1_R2(ref L, ref A);
            case 0x70:
                return LD_ARR_R(ref B, "hl");
            case 0x71:
                return LD_ARR_R(ref C, "hl");
            case 0x72:
                return LD_ARR_R(ref D, "hl");
            case 0x73:
                return LD_ARR_R(ref E, "hl");
            case 0x74:
                return LD_ARR_R(ref H, "hl");
            case 0x75:
                return LD_ARR_R(ref L, "hl");
            case 0x76:
                return HALT();
            case 0x77:
                return LD_ARR_R(ref A, "hl");
            case 0x78:
                return LD_R1_R2(ref A, ref B);
            case 0x79:
                return LD_R1_R2(ref A, ref C);
            case 0x7A:
                return LD_R1_R2(ref A, ref D);
            case 0x7B:
                return LD_R1_R2(ref A, ref E);
            case 0x7C:
                return LD_R1_R2(ref A, ref H);
            case 0x7D:
                return LD_R1_R2(ref A, ref L);
            case 0x7E:
                return LD_R_ARR(ref A, "hl");
            case 0x7F:
                return LD_R1_R2(ref A, ref A);
            case 0x80:
                return ADD_A_R(ref B);
            case 0x81:
                return ADD_A_R(ref C);
            case 0x82:
                return ADD_A_R(ref D);
            case 0x83:
                return ADD_A_R(ref E);
            case 0x84:
                return ADD_A_R(ref H);
            case 0x85:
                return ADD_A_R(ref L);
            case 0x86:
                return ADD_A_ARR("hl");
            case 0x87:
                return ADD_A_R(ref A);
            case 0x88:
                return ADC_A_R(ref B);
            case 0x89:
                return ADC_A_R(ref C);
            case 0x8A:
                return ADC_A_R(ref D);
            case 0x8B:
                return ADC_A_R(ref E);
            case 0x8C:
                return ADC_A_R(ref H);
            case 0x8D:
                return ADC_A_R(ref L);
            case 0x8E:
                return ADC_A_ARR("hl");
            case 0x8F:
                return ADC_A_R(ref A);
            case 0x90:
                return SUB_A_R(ref B);
            case 0x91:
                return SUB_A_R(ref C);
            case 0x92:
                return SUB_A_R(ref D);
            case 0x93:
                return SUB_A_R(ref E);
            case 0x94:
                return SUB_A_R(ref H);
            case 0x95:
                return SUB_A_R(ref L);
            case 0x96:
                return SUB_A_ARR("hl");
            case 0x97:
                return SUB_A_R(ref A);
            case 0x98:
                return SBC_A_R(ref B);
            case 0x99:
                return SBC_A_R(ref C);
            case 0x9A:
                return SBC_A_R(ref D);
            case 0x9B:
                return SBC_A_R(ref E);
            case 0x9C:
                return SBC_A_R(ref H);
            case 0x9D:
                return SBC_A_R(ref L);
            case 0x9E:
                return SBC_A_ARR("hl");
            case 0x9F:
                return SBC_A_R(ref A);
            case 0xA0:
                return AND_A_R(ref B);
            case 0xA1:
                return AND_A_R(ref C);
            case 0xA2:
                return AND_A_R(ref D);
            case 0xA3:
                return AND_A_R(ref E);
            case 0xA4:
                return AND_A_R(ref H);
            case 0xA5:
                return AND_A_R(ref L);
            case 0xA6:
                return AND_A_ARR("hl");
            case 0xA7:
                return AND_A_R(ref A);
            case 0xA8:
                return XOR_A_R(ref B);
            case 0xA9:
                return XOR_A_R(ref C);
            case 0xAA:
                return XOR_A_R(ref D);
            case 0xAB:
                return XOR_A_R(ref E);
            case 0xAC:
                return XOR_A_R(ref H);
            case 0xAD:
                return XOR_A_R(ref L);
            case 0xAE:
                return XOR_A_ARR("hl");
            case 0xAF:
                return XOR_A_R(ref A);
            case 0xB0:
                return OR_A_R(ref B);
            case 0xB1:
                return OR_A_R(ref C);
            case 0xB2:
                return OR_A_R(ref D);
            case 0xB3:
                return OR_A_R(ref E);
            case 0xB4:
                return OR_A_R(ref H);
            case 0xB5:
                return OR_A_R(ref L);
            case 0xB6:
                return OR_A_ARR("hl");
            case 0xB7:
                return OR_A_R(ref A);
            case 0xB8:
                return CP_A_R(ref B);
            case 0xB9:
                return CP_A_R(ref C);
            case 0xBA:
                return CP_A_R(ref D);
            case 0xBB:
                return CP_A_R(ref E);
            case 0xBC:
                return CP_A_R(ref H);
            case 0xBD:
                return CP_A_R(ref L);
            case 0xBE:
                return CP_A_ARR("hl");
            case 0xBF:
                return CP_A_R(ref A);
            case 0xC0:
                return RET_CON(!zero);
            case 0xC1:
                return POP_RR("bc");
            case 0xC2:
                return JP_CON_U16(!zero);
            case 0xC3:
                return JP_CON_U16(true);
            case 0xC4:
                return CALL_CON_U16(!zero);
            case 0xC5:
                return PUSH_RR("bc");
            case 0xC6:
                return ADD_A_U8();
            case 0xC7:
                return RST(0x0000);
            case 0xC8:
                return RET_CON(zero);
            case 0xC9:
                return RET();
            case 0xCA:
                return JP_CON_U16(zero);
            case 0xCB:
                return ExecuteCB();
            case 0xCC:
                return CALL_CON_U16(zero);
            case 0xCD:
                return CALL_U16();
            case 0xCE:
                return ADC_A_U8();
            case 0xCF:
                return RST(0x0008);
            case 0xD0:
                return RET_CON(!carry);
            case 0xD1:
                return POP_RR("de");
            case 0xD2:
                return JP_CON_U16(!carry);
            case 0xD3:
                return DMG_EXIT(opcode);
            case 0xD4:
                return CALL_CON_U16(!carry);
            case 0xD5:
                return PUSH_RR("de");
            case 0xD6:
                return SUB_A_U8();
            case 0xD7:
                return RST(0x0010);
            case 0xD8:
                return RET_CON(carry);
            case 0xD9:
                return RETI();
            case 0xDA:
                return JP_CON_U16(carry);
            case 0xDB:
                return DMG_EXIT(opcode);
            case 0xDC:
                return CALL_CON_U16(carry);
            case 0xDD:
                return DMG_EXIT(opcode);
            case 0xDE:
                return SBC_A_U8();
            case 0xDF:
                return RST(0x0018);
            case 0xE0:
                return LD_FF00_U8_A();
            case 0xE1:
                return POP_RR("hl");
            case 0xE2:
                return LD_FF00_C_A();
            case 0xE3:
                return DMG_EXIT(opcode);
            case 0xE4:
                return DMG_EXIT(opcode);
            case 0xE5:
                return PUSH_RR("hl");
            case 0xE6:
                return AND_A_U8();
            case 0xE7:
                return RST(0x0020);
            case 0xE8:
                return ADD_SP_I8();
            case 0xE9:
                return JP_HL();
            case 0xEA:
                return LD_AU16_A();
            case 0xEB:
                return DMG_EXIT(opcode);
            case 0xEC:
                return DMG_EXIT(opcode);
            case 0xED:
                return DMG_EXIT(opcode);
            case 0xEE:
                return XOR_A_U8();
            case 0xEF:
                return RST(0x0028);
            case 0xF0:
                return LD_A_FF00_U8();
            case 0xF1:
                return POP_AF();
            case 0xF2:
                return LD_A_FF00_C();
            case 0xF3:
                return DI();
            case 0xF4:
                return DMG_EXIT(opcode);
            case 0xF5:
                return PUSH_RR("af");
            case 0xF6:
                return OR_A_U8();
            case 0xF7:
                return RST(0x0030);
            case 0xF8:
                return LD_HL_SP_I8();
            case 0xF9:
                return LD_SP_HL();
            case 0xFA:
                return LD_A_AU16();
            case 0xFB:
                return EI();
            case 0xFC:
                return DMG_EXIT(opcode);
            case 0xFD:
                return DMG_EXIT(opcode);
            case 0xFE:
                return CP_A_U8();
            case 0xFF:
                return RST(0x0038);
            default:
                //Console.WriteLine("Unimplemented Opcode: " + opcode.ToString("X2") + " , PC: " + (PC-1).ToString("X4"));
                //Environment.Exit(1);
                //return 0;
        }
    }

    public int ExecuteCB() {
        byte suffix = Fetch();

        switch (suffix) {
            case 0x00:
                return RLC_R(ref B);
            case 0x01:
                return RLC_R(ref C);
            case 0x02:
                return RLC_R(ref D);
            case 0x03:
                return RLC_R(ref E);
            case 0x04:
                return RLC_R(ref H);
            case 0x05:
                return RLC_R(ref L);
            case 0x06:
                return RLC_AHL();
            case 0x07:
                return RLC_R(ref A);
            case 0x08:
                return RRC_R(ref B);
            case 0x09:
                return RRC_R(ref C);
            case 0x0A:
                return RRC_R(ref D);
            case 0x0B:
                return RRC_R(ref E);
            case 0x0C:
                return RRC_R(ref H);
            case 0x0D:
                return RRC_R(ref L);
            case 0x0E:
                return RRC_AHL();
            case 0x0F:
                return RRC_R(ref A);
            case 0x10:
                return RL_R(ref B);
            case 0x11:
                return RL_R(ref C);
            case 0x12:
                return RL_R(ref D);
            case 0x13:
                return RL_R(ref E);
            case 0x14:
                return RL_R(ref H);
            case 0x15:
                return RL_R(ref L);
            case 0x16:
                return RL_AHL();
            case 0x17:
                return RL_R(ref A);
            case 0x18:
                return RR_R(ref B);
            case 0x19:
                return RR_R(ref C);
            case 0x1A:
                return RR_R(ref D);
            case 0x1B:
                return RR_R(ref E);
            case 0x1C:
                return RR_R(ref H);
            case 0x1D:
                return RR_R(ref L);
            case 0x1E:
                return RR_AHL();
            case 0x1F:
                return RR_R(ref A);
            case 0x20:
                return SLA_R(ref B);
            case 0x21:
                return SLA_R(ref C);
            case 0x22:
                return SLA_R(ref D);
            case 0x23:
                return SLA_R(ref E);
            case 0x24:
                return SLA_R(ref H);
            case 0x25:
                return SLA_R(ref L);
            case 0x26:
                return SLA_AHL();
            case 0x27:
                return SLA_R(ref A);
            case 0x28:
                return SRA_R(ref B);
            case 0x29:
                return SRA_R(ref C);
            case 0x2A:
                return SRA_R(ref D);
            case 0x2B:
                return SRA_R(ref E);
            case 0x2C:
                return SRA_R(ref H);
            case 0x2D:
                return SRA_R(ref L);
            case 0x2E:
                return SRA_AHL();
            case 0x2F:
                return SRA_R(ref A);
            case 0x30:
                return SWAP_R(ref B);
            case 0x31:
                return SWAP_R(ref C);
            case 0x32:
                return SWAP_R(ref D);
            case 0x33:
                return SWAP_R(ref E);
            case 0x34:
                return SWAP_R(ref H);
            case 0x35:
                return SWAP_R(ref L);
            case 0x36:
                return SWAP_AHL();
            case 0x37:
                return SWAP_R(ref A);
            case 0x38:
                return SRL_R(ref B);
            case 0x39:
                return SRL_R(ref C);
            case 0x3A:
                return SRL_R(ref D);
            case 0x3B:
                return SRL_R(ref E);
            case 0x3C:
                return SRL_R(ref H);
            case 0x3D:
                return SRL_R(ref L);
            case 0x3E:
                return SRL_AHL();
            case 0x3F:
                return SRL_R(ref A);
            case 0x40:
                return BIT_N_R(0, ref B);
            case 0x41:
                return BIT_N_R(0, ref C);
            case 0x42:
                return BIT_N_R(0, ref D);
            case 0x43:
                return BIT_N_R(0, ref E);
            case 0x44:
                return BIT_N_R(0, ref H);
            case 0x45:
                return BIT_N_R(0, ref L);
            case 0x46:
                return BIT_N_AHL(0);
            case 0x47:
                return BIT_N_R(0, ref A);
            case 0x48:
                return BIT_N_R(1, ref B);
            case 0x49:
                return BIT_N_R(1, ref C);
            case 0x4A:
                return BIT_N_R(1, ref D);
            case 0x4B:
                return BIT_N_R(1, ref E);
            case 0x4C:
                return BIT_N_R(1, ref H);
            case 0x4D:
                return BIT_N_R(1, ref L);
            case 0x4E:
                return BIT_N_AHL(1);
            case 0x4F:
                return BIT_N_R(1, ref A);
            case 0x50:
                return BIT_N_R(2, ref B);
            case 0x51:
                return BIT_N_R(2, ref C);
            case 0x52:
                return BIT_N_R(2, ref D);
            case 0x53:
                return BIT_N_R(2, ref E);
            case 0x54:
                return BIT_N_R(2, ref H);
            case 0x55:
                return BIT_N_R(2, ref L);
            case 0x56:
                return BIT_N_AHL(2);
            case 0x57:
                return BIT_N_R(2, ref A);
            case 0x58:
                return BIT_N_R(3, ref B);
            case 0x59:
                return BIT_N_R(3, ref C);
            case 0x5A:
                return BIT_N_R(3, ref D);
            case 0x5B:
                return BIT_N_R(3, ref E);
            case 0x5C:
                return BIT_N_R(3, ref H);
            case 0x5D:
                return BIT_N_R(3, ref L);
            case 0x5E:
                return BIT_N_AHL(3);
            case 0x5F:
                return BIT_N_R(3, ref A);
            case 0x60:
                return BIT_N_R(4, ref B);
            case 0x61:
                return BIT_N_R(4, ref C);
            case 0x62:
                return BIT_N_R(4, ref D);
            case 0x63:
                return BIT_N_R(4, ref E);
            case 0x64:
                return BIT_N_R(4, ref H);
            case 0x65:
                return BIT_N_R(4, ref L);
            case 0x66:
                return BIT_N_AHL(4);
            case 0x67:
                return BIT_N_R(4, ref A);
            case 0x68:
                return BIT_N_R(5, ref B);
            case 0x69:
                return BIT_N_R(5, ref C);
            case 0x6A:
                return BIT_N_R(5, ref D);
            case 0x6B:
                return BIT_N_R(5, ref E);
            case 0x6C:
                return BIT_N_R(5, ref H);
            case 0x6D:
                return BIT_N_R(5, ref L);
            case 0x6E:
                return BIT_N_AHL(5);
            case 0x6F:
                return BIT_N_R(5, ref A);
            case 0x70:
                return BIT_N_R(6, ref B);
            case 0x71:
                return BIT_N_R(6, ref C);
            case 0x72:
                return BIT_N_R(6, ref D);
            case 0x73:
                return BIT_N_R(6, ref E);
            case 0x74:
                return BIT_N_R(6, ref H);
            case 0x75:
                return BIT_N_R(6, ref L);
            case 0x76:
                return BIT_N_AHL(6);
            case 0x77:
                return BIT_N_R(6, ref A);
            case 0x78:
                return BIT_N_R(7, ref B);
            case 0x79:
                return BIT_N_R(7, ref C);
            case 0x7A:
                return BIT_N_R(7, ref D);
            case 0x7B:
                return BIT_N_R(7, ref E);
            case 0x7C:
                return BIT_N_R(7, ref H);
            case 0x7D:
                return BIT_N_R(7, ref L);
            case 0x7E:
                return BIT_N_AHL(7);
            case 0x7F:
                return BIT_N_R(7, ref A);
            case 0x80:
                return RES_N_R(0, ref B);
            case 0x81:
                return RES_N_R(0, ref C);
            case 0x82:
                return RES_N_R(0, ref D);
            case 0x83:
                return RES_N_R(0, ref E);
            case 0x84:
                return RES_N_R(0, ref H);
            case 0x85:
                return RES_N_R(0, ref L);
            case 0x86:
                return RES_N_AHL(0);
            case 0x87:
                return RES_N_R(0, ref A);
            case 0x88:
                return RES_N_R(1, ref B);
            case 0x89:
                return RES_N_R(1, ref C);
            case 0x8A:
                return RES_N_R(1, ref D);
            case 0x8B:
                return RES_N_R(1, ref E);
            case 0x8C:
                return RES_N_R(1, ref H);
            case 0x8D:
                return RES_N_R(1, ref L);
            case 0x8E:
                return RES_N_AHL(1);
            case 0x8F:
                return RES_N_R(1, ref A);
            case 0x90:
                return RES_N_R(2, ref B);
            case 0x91:
                return RES_N_R(2, ref C);
            case 0x92:
                return RES_N_R(2, ref D);
            case 0x93:
                return RES_N_R(2, ref E);
            case 0x94:
                return RES_N_R(2, ref H);
            case 0x95:
                return RES_N_R(2, ref L);
            case 0x96:
                return RES_N_AHL(2);
            case 0x97:
                return RES_N_R(2, ref A);
            case 0x98:
                return RES_N_R(3, ref B);
            case 0x99:
                return RES_N_R(3, ref C);
            case 0x9A:
                return RES_N_R(3, ref D);
            case 0x9B:
                return RES_N_R(3, ref E);
            case 0x9C:
                return RES_N_R(3, ref H);
            case 0x9D:
                return RES_N_R(3, ref L);
            case 0x9E:
                return RES_N_AHL(3);
            case 0x9F:
                return RES_N_R(3, ref A);
            case 0xA0:
                return RES_N_R(4, ref B);
            case 0xA1:
                return RES_N_R(4, ref C);
            case 0xA2:
                return RES_N_R(4, ref D);
            case 0xA3:
                return RES_N_R(4, ref E);
            case 0xA4:
                return RES_N_R(4, ref H);
            case 0xA5:
                return RES_N_R(4, ref L);
            case 0xA6:
                return RES_N_AHL(4);
            case 0xA7:
                return RES_N_R(4, ref A);
            case 0xA8:
                return RES_N_R(5, ref B);
            case 0xA9:
                return RES_N_R(5, ref C);
            case 0xAA:
                return RES_N_R(5, ref D);
            case 0xAB:
                return RES_N_R(5, ref E);
            case 0xAC:
                return RES_N_R(5, ref H);
            case 0xAD:
                return RES_N_R(5, ref L);
            case 0xAE:
                return RES_N_AHL(5);
            case 0xAF:
                return RES_N_R(5, ref A);
            case 0xB0:
                return RES_N_R(6, ref B);
            case 0xB1:
                return RES_N_R(6, ref C);
            case 0xB2:
                return RES_N_R(6, ref D);
            case 0xB3:
                return RES_N_R(6, ref E);
            case 0xB4:
                return RES_N_R(6, ref H);
            case 0xB5:
                return RES_N_R(6, ref L);
            case 0xB6:
                return RES_N_AHL(6);
            case 0xB7:
                return RES_N_R(6, ref A);
            case 0xB8:
                return RES_N_R(7, ref B);
            case 0xB9:
                return RES_N_R(7, ref C);
            case 0xBA:
                return RES_N_R(7, ref D);
            case 0xBB:
                return RES_N_R(7, ref E);
            case 0xBC:
                return RES_N_R(7, ref H);
            case 0xBD:
                return RES_N_R(7, ref L);
            case 0xBE:
                return RES_N_AHL(7);
            case 0xBF:
                return RES_N_R(7, ref A);
            case 0xC0:
                return SET_N_R(0, ref B);
            case 0xC1:
                return SET_N_R(0, ref C);
            case 0xC2:
                return SET_N_R(0, ref D);
            case 0xC3:
                return SET_N_R(0, ref E);
            case 0xC4:
                return SET_N_R(0, ref H);
            case 0xC5:
                return SET_N_R(0, ref L);
            case 0xC6:
                return SET_N_AHL(0);
            case 0xC7:
                return SET_N_R(0, ref A);
            case 0xC8:
                return SET_N_R(1, ref B);
            case 0xC9:
                return SET_N_R(1, ref C);
            case 0xCA:
                return SET_N_R(1, ref D);
            case 0xCB:
                return SET_N_R(1, ref E);
            case 0xCC:
                return SET_N_R(1, ref H);
            case 0xCD:
                return SET_N_R(1, ref L);
            case 0xCE:
                return SET_N_AHL(1);
            case 0xCF:
                return SET_N_R(1, ref A);
            case 0xD0:
                return SET_N_R(2, ref B);
            case 0xD1:
                return SET_N_R(2, ref C);
            case 0xD2:
                return SET_N_R(2, ref D);
            case 0xD3:
                return SET_N_R(2, ref E);
            case 0xD4:
                return SET_N_R(2, ref H);
            case 0xD5:
                return SET_N_R(2, ref L);
            case 0xD6:
                return SET_N_AHL(2);
            case 0xD7:
                return SET_N_R(2, ref A);
            case 0xD8:
                return SET_N_R(3, ref B);
            case 0xD9:
                return SET_N_R(3, ref C);
            case 0xDA:
                return SET_N_R(3, ref D);
            case 0xDB:
                return SET_N_R(3, ref E);
            case 0xDC:
                return SET_N_R(3, ref H);
            case 0xDD:
                return SET_N_R(3, ref L);
            case 0xDE:
                return SET_N_AHL(3);
            case 0xDF:
                return SET_N_R(3, ref A);
            case 0xE0:
                return SET_N_R(4, ref B);
            case 0xE1:
                return SET_N_R(4, ref C);
            case 0xE2:
                return SET_N_R(4, ref D);
            case 0xE3:
                return SET_N_R(4, ref E);
            case 0xE4:
                return SET_N_R(4, ref H);
            case 0xE5:
                return SET_N_R(4, ref L);
            case 0xE6:
                return SET_N_AHL(4);
            case 0xE7:
                return SET_N_R(4, ref A);
            case 0xE8:
                return SET_N_R(5, ref B);
            case 0xE9:
                return SET_N_R(5, ref C);
            case 0xEA:
                return SET_N_R(5, ref D);
            case 0xEB:
                return SET_N_R(5, ref E);
            case 0xEC:
                return SET_N_R(5, ref H);
            case 0xED:
                return SET_N_R(5, ref L);
            case 0xEE:
                return SET_N_AHL(5);
            case 0xEF:
                return SET_N_R(5, ref A);
            case 0xF0:
                return SET_N_R(6, ref B);
            case 0xF1:
                return SET_N_R(6, ref C);
            case 0xF2:
                return SET_N_R(6, ref D);
            case 0xF3:
                return SET_N_R(6, ref E);
            case 0xF4:
                return SET_N_R(6, ref H);
            case 0xF5:
                return SET_N_R(6, ref L);
            case 0xF6:
                return SET_N_AHL(6);
            case 0xF7:
                return SET_N_R(6, ref A);
            case 0xF8:
                return SET_N_R(7, ref B);
            case 0xF9:
                return SET_N_R(7, ref C);
            case 0xFA:
                return SET_N_R(7, ref D);
            case 0xFB:
                return SET_N_R(7, ref E);
            case 0xFC:
                return SET_N_R(7, ref H);
            case 0xFD:
                return SET_N_R(7, ref L);
            case 0xFE:
                return SET_N_AHL(7);
            case 0xFF:
                return SET_N_R(7, ref A);
            default:
                //Console.WriteLine("Unimplemented CB Opcode: " + suffix.ToString("X2") + " , PC: " + (PC-1).ToString("X4"));
                //Environment.Exit(1);
                //return 0;
        }
    }

    //R = 8bit reg, RR = 16bit reg, U8 = byte, U16 = ushort, I8, sbyte, AXX = (XX) Value in memory at XX

    //x8 LSM
    private int LD_ARR_R(ref byte r, string regPair) {
        ushort addr = Get16BitReg(regPair);
        mmu.Write(addr, r);
        return 8;
    }

    private int LD_AHLM_A() {
        ushort hlm = Get16BitReg("hl");
        mmu.Write(hlm, A);
        hlm = (ushort)(hlm - 1);
        Load16BitReg("hl", hlm);
        return 8;
    }

    private int LD_AHLI_A() {
        ushort hli = Get16BitReg("hl");
        mmu.Write(hli, A);
        hli = (ushort)(hli + 1);
        Load16BitReg("hl", hli);
        return 8;
    }

    private int LD_R_U8(ref byte r) {
        byte u8 = Fetch();
        r = u8;
        return 8;
    }

    private int LD_AHL_U8() {
        ushort addr = Get16BitReg("hl");
        byte u8 = Fetch();
        mmu.Write(addr, u8);
        return 12;
    }

    private int LD_R_ARR(ref byte r, string regPair) {
        ushort addr = Get16BitReg(regPair);
        r = mmu.Read(addr);
        return 8;
    }

    private int LD_A_AHLM() {
        ushort hlm = Get16BitReg("hl");
        A = mmu.Read(hlm);
        hlm = (ushort)(hlm - 1);
        Load16BitReg("hl", hlm);
        return 8;
    }

    private int LD_A_AHLI() {
        ushort hli = Get16BitReg("hl");
        A = mmu.Read(hli);
        hli = (ushort)(hli + 1);
        Load16BitReg("hl", hli);
        return 8;
    }

    private int LD_R1_R2(ref byte r1, ref byte r2) {
        r1 = r2;
        return 4;
    }

    private int LD_FF00_U8_A() {
        byte u8 = Fetch();
        ushort addr = (ushort)(0xFF00 + u8);
        mmu.Write(addr, A);
        return 12;
    }

    private int LD_A_FF00_U8() {
        byte u8 = Fetch();
        ushort addr = (ushort)(0xFF00 + u8);
        A = mmu.Read(addr);
        return 12;
    }

    private int LD_FF00_C_A() {
        ushort addr = (ushort)(0xFF00 + C);
        mmu.Write(addr, A);
        return 8;
    }

    private int LD_A_FF00_C() {
        ushort addr = (ushort)(0xFF00 + C);
        A = mmu.Read(addr);
        return 8;
    }

    private int LD_AU16_A() {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort value = (ushort)((upper << 8) | lower);
        mmu.Write(value, A);
        return 16;
    }  

    private int LD_A_AU16() {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort value = (ushort)((upper << 8) | lower);
        A = mmu.Read(value);
        return 16;
    }

    //x16 LSM
    private int LD_SP_U16() {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort value = (ushort)((upper << 8) | lower);

        SP = value;
        return 12;
    }

    private int LD_RR_U16(ref byte r1, ref byte r2) {
        r2 = Fetch();
        r1 = Fetch();
        return 12;
    }

    private int LD_AU16_SP() {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort address = (ushort)((upper << 8) | lower);

        byte spLower = (byte)(SP & 0xFF);
        byte spUpper = (byte)((SP >> 8) & 0xFF);
        mmu.Write(address, spLower);
        mmu.Write((ushort)(address + 1), spUpper);

        return 20;
    }

    private int PUSH_RR(string regPair) {
        ushort regVal = Get16BitReg(regPair);
        SP--;
        mmu.Write(SP, (byte)(regVal >> 8));
        SP--;
        mmu.Write(SP, (byte)(regVal & 0xFF));
        return 16;
    }

    private int POP_RR(string regPair) {
        byte lowByte = mmu.Read(SP);
        SP++;
        byte highByte = mmu.Read(SP);
        ushort regVal = (ushort)((highByte << 8) | lowByte);
        Load16BitReg(regPair, regVal);
        SP++;
        return 12;
    }

    private int POP_AF() {
        byte lowByte = mmu.Read(SP);
        SP++;
        byte highByte = mmu.Read(SP);
        A = highByte;
        F = (byte)(lowByte & 0xF0);
        UpdateFlagsFromF();
        SP++;
        return 12;
    }

    private int LD_SP_HL() {
        SP = Get16BitReg("hl");
        return 8;
    }

    //x8 ALU
    private int INC_R(ref byte r) {
        int val = r + 1;

        zero = (val & 0xFF) == 0;
        negative = false;
        halfCarry = (val & 0x0F) == 0;
        UpdateFFromFlags();

        r = (byte)val;
        return 4;
    }

    private int INC_AHL() {
        ushort addr = Get16BitReg("hl");
        byte val = mmu.Read(addr);
        int result = val + 1;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = (result & 0x0F) == 0;
        UpdateFFromFlags();

        mmu.Write(addr, (byte)result);
        return 12;
    }

    private int DEC_R(ref byte r) {
        int val = r - 1;

        zero = (val & 0xFF) == 0; 
        negative = true;
        halfCarry = (r & 0x0F) == 0;
        UpdateFFromFlags();

        r = (byte)val;
        return 4;
    }

    private int DEC_AHL() {
        ushort addr = Get16BitReg("hl");
        byte val = mmu.Read(addr);
        int result = val - 1;

        zero = (result & 0xFF) == 0; 
        negative = true;
        halfCarry = (val & 0x0F) == 0;
        UpdateFFromFlags();

        mmu.Write(addr, (byte)result);
        return 12;
    }

    private int ADD_A_R(ref byte r) {
        int val = A + r;

        zero = (val & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (r & 0xF)) > 0xF;
        carry = val > 0xFF;
        UpdateFFromFlags();

        A = (byte)(val & 0xFF);
        return 4;
    }

    private int ADD_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);    
        int result = A + val;

        zero = (result & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (val & 0xF)) > 0xF;
        carry = result > 0xFF;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int ADD_A_U8() {
        byte val = Fetch();
        int result = A + val;

        zero = (result & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (val & 0xF)) > 0xF;
        carry = result > 0xFF;
        UpdateFFromFlags();

        A = (byte)(result & 0xFF);
        return 8;
    }

    private int ADC_A_R(ref byte r) {
        int val = A + r + (carry ? 1 : 0);

        zero = (val & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (r & 0xF) + (carry ? 1 : 0)) > 0xF;
        carry = val > 0xFF;
        UpdateFFromFlags();

        A = (byte)(val & 0xFF);
        return 4;
    }

    private int ADC_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);    
        int result = A + val + (carry ? 1 : 0);

        zero = (result & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (val & 0xF) + (carry ? 1 : 0)) > 0xF;
        carry = result > 0xFF;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int ADC_A_U8() {
        byte val = Fetch();
        int result = A + val + (carry ? 1 : 0);

        zero = (result & 0xFF) == 0;
        negative = false;                  
        halfCarry = ((A & 0xF) + (val & 0xF) + (carry ? 1 : 0)) > 0xF;
        carry = result > 0xFF;
        UpdateFFromFlags();

        A = (byte)(result & 0xFF);
        return 8;
    }

    private int SUB_A_R(ref byte r) {
        int val = A - r;

        zero = val == 0;
        negative = true;
        halfCarry = (A & 0xF) < (r & 0xF);
        carry = A < r;
        UpdateFFromFlags();

        A = (byte)val;
        return 4;
    }

    private int SUB_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair); 
        byte val = mmu.Read(addr);    
        int result = A - val;

        zero = (result & 0xFF) == 0;
        negative = true;
        halfCarry = (A & 0xF) < (val & 0xF);
        carry = result < 0;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int SUB_A_U8() {
        byte val = Fetch();
        int result = A - val;

        zero = result == 0;
        negative = true;
        halfCarry = (A & 0xF) < (val & 0xF);
        carry = A < val;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int SBC_A_R(ref byte r) {
        int val = A - r - (carry ? 1 : 0);

        zero = (val & 0xFF) == 0;
        negative = true;
        halfCarry = ((A & 0xF) - (r & 0xF) - (carry ? 1 : 0)) < 0;
        carry = val < 0;
        UpdateFFromFlags();

        A = (byte)val;
        return 4;
    }

    private int SBC_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair); 
        byte val = mmu.Read(addr);    
        int result = A - val - (carry ? 1 : 0);

        zero = (result & 0xFF) == 0;
        negative = true;
        halfCarry = ((A & 0xF) - (val & 0xF) - (carry ? 1 : 0)) < 0;
        carry = result < 0;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int SBC_A_U8() {
        byte val = Fetch();
        int result = A - val - (carry ? 1 : 0);

        zero = (result & 0xFF) == 0;
        negative = true;
        halfCarry = ((A & 0xF) - (val & 0xF) - (carry ? 1 : 0)) < 0;
        carry = result < 0;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int AND_A_R(ref byte r) {
        int val = A & r;

        zero = (val & 0xFF) == 0;
        negative = false;
        halfCarry = true;
        carry = false;
        UpdateFFromFlags();

        A = (byte)val;
        return 4;
    }

    private int AND_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);
        int result = A & val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = true;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int AND_A_U8() {
        byte val = Fetch();
        int result = A & val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = true;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int XOR_A_R(ref byte r) {
        int val = A ^ r;

        //zero = A == 0;
        zero = (val & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)val;
        return 4;
    }

    private int XOR_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);
        int result = A ^ val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int XOR_A_U8() {
        byte val = Fetch();
        int result = A ^ val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int OR_A_R(ref byte r) {
        int val = A | r;

        zero = (val & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)val;
        return 4;
    }

    private int OR_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);
        int result = A | val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int OR_A_U8() {
        byte val = Fetch();
        int result = A | val;

        zero = (result & 0xFF) == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        A = (byte)result;
        return 8;
    }

    private int CP_A_R(ref byte r) {
        int result = A - r;

        zero = (result & 0xFF) == 0;
        negative = true;
        halfCarry = (A & 0xF) < (r & 0xF);
        carry = result < 0;
        UpdateFFromFlags();

        return 4;
    }

    private int CP_A_ARR(string regPair) {
        ushort addr = Get16BitReg(regPair);
        byte val = mmu.Read(addr);
        int result = A - val;

        zero = result == 0;
        negative = true;
        halfCarry = (A & 0xF) < (val & 0xF);
        carry = A < val;
        UpdateFFromFlags();

        return 8;
    }

    private int CP_A_U8() {
        byte val = Fetch();
        int result = A - val;

        zero = result == 0;
        negative = true;
        halfCarry = (A & 0xF) < (val & 0xF);
        carry = A < val;
        UpdateFFromFlags();

        return 8;
    }

    private int CPL() {
        A = (byte)~A;

        negative = true;
        halfCarry = true;
        UpdateFFromFlags();

        return 4;
    }

    private int SCF() {
        carry = true;
        negative = false;
        halfCarry = false;
        UpdateFFromFlags();

        return 4;
    }

    private int CCF() {
        carry = !carry;
        negative = false;
        halfCarry = false;
        UpdateFFromFlags();

        return 4;
    }

    private int DAA() {
        byte adjust = (byte)(carry ? 0x60 : 0x00);
        if (halfCarry) {
            adjust |= 0x06;
        }
        if (negative) {
            A -= adjust;
        } else {
            if ((A & 0x0F) > 0x09) {
                adjust |= 0x06;
            }
            if (A > 0x99) {
                adjust |= 0x60;
            }

            A += adjust;
        }

        zero = A == 0;
        halfCarry = false;
        carry = adjust >= 0x60;
        UpdateFFromFlags();

        return 4;
    }

    //x16 ALU
    private int INC_RR(string regPair) {
        ushort regVal = Get16BitReg(regPair);
        regVal++;
        Load16BitReg(regPair, regVal);
        return 8;
    }

    private int INC_SP() {
        SP++;
        return 8;
    }

    private int DEC_RR(string regPair) {
        ushort regVal = Get16BitReg(regPair);
        regVal--;
        Load16BitReg(regPair, regVal);
        return 8;
    }

    private int DEC_SP() {
        SP--;
        return 8;
    }

    private int ADD_HL_RR(string regPair) {
        ushort hl = Get16BitReg("hl");
        ushort regVal = Get16BitReg(regPair);
        int result = hl + regVal;

        negative = false;
        halfCarry = ((hl & 0x0FFF) + (regVal & 0x0FFF)) > 0x0FFF;
        carry = result > 0xFFFF;
        UpdateFFromFlags();

        Load16BitReg("hl", (ushort)result);

        return 8;
    }

    private int ADD_HL_SP() {
        ushort hl = Get16BitReg("hl");
        int result = hl + SP;

        negative = false;
        halfCarry = ((hl & 0x0FFF) + (SP & 0x0FFF)) > 0x0FFF;
        carry = result > 0xFFFF;
        UpdateFFromFlags();

        Load16BitReg("hl", (ushort)result);

        return 8;
    }

    private int ADD_SP_I8() {
        byte lower = (byte)(SP & 0xFF);
        byte high = (byte)((SP >> 8) & 0xFF);
        sbyte sb = (sbyte)Fetch();
        int result = SP + (ushort)sb;

        zero = false;
        negative = false;
        halfCarry = ((SP ^ sb ^ (result & 0xFFFF)) & 0x10) == 0x10;
        carry = ((SP ^ sb ^ (result & 0xFFFF)) & 0x100) == 0x100;
        UpdateFFromFlags();

        SP = (ushort)result;

        return 16;
    }

    private int LD_HL_SP_I8() {
        sbyte sb = (sbyte)Fetch();
        int result = SP + sb;

        zero = false;
        negative = false;
        halfCarry = ((SP ^ sb ^ (result & 0xFFFF)) & 0x10) == 0x10;
        carry = ((SP ^ sb ^ (result & 0xFFFF)) & 0x100) == 0x100;
        UpdateFFromFlags();

        Load16BitReg("hl", (ushort)result);

        return 12;
    }

    //x8 RSB
    private int RLCA() {
        byte wrap = (byte)(A & 0x80);
        byte result = (byte)((A << 1) | (wrap >> 7));

        zero = false;
        negative = false;
        halfCarry = false;
        carry = (A & 0x80) != 0;
        UpdateFFromFlags();

        A = result;

        return 4;
    }

    private int RRCA() {
        byte wrap = (byte)(A & 0x01);
        byte result = (byte)((A >> 1) | (wrap << 7));

        zero = false;
        negative = false;
        halfCarry = false;
        carry = (A & 0x01) != 0;
        UpdateFFromFlags();

        A = result;

        return 4;
    }

    private int RLA() {
        byte carryRLA = (byte)(carry ? 1 : 0);
        byte result = (byte)((A << 1) | carryRLA);

        zero = false;
        negative = false;
        halfCarry = false;
        carry = (A & 0x80) != 0;
        UpdateFFromFlags();

        A = result;
        return 4;
    }

    private int RRA() {
        byte carryRRA = (byte)(carry ? 1 : 0);
        byte result = (byte)((A >> 1) | (carryRRA << 7));

        zero = false;
        negative = false;
        halfCarry = false;
        carry = (A & 0x01) != 0;
        UpdateFFromFlags();

        A = result;
        return 4;
    }

    private int RLC_R(ref byte r) {
        byte wrap = (byte)(r & 0x80);
        byte result = (byte)((r << 1) | (wrap >> 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (r & 0x80) != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int RLC_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte wrap = (byte)(val & 0x80);
        byte result = (byte)((val << 1) | (wrap >> 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (val & 0x80) != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int RRC_R(ref byte r) {
        byte wrap = (byte)(r & 0x01);
        byte result = (byte)((r >> 1) | (wrap << 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (r & 0x01) != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int RRC_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte wrap = (byte)(val & 0x01);
        byte result = (byte)((val >> 1) | (wrap << 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (val & 0x01) != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int RL_R(ref byte r) {
        byte carryRL = (byte)(carry ? 1 : 0);
        byte result = (byte)((r << 1) | carryRL);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (r & 0x80) != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int RL_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address); 
        byte carryRL = (byte)(carry ? 1 : 0);
        byte result = (byte)((val << 1) | carryRL);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (val & 0x80) != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int RR_R(ref byte r) {
        byte carryRR = (byte)(carry ? 1 : 0);
        byte result = (byte)((r >> 1) | (carryRR << 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (r & 0x01) != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int RR_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte carryRR = (byte)(carry ? 1 : 0);
        byte result = (byte)((val >> 1) | (carryRR << 7));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = (val & 0x01) != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int SLA_R(ref byte r) {
        byte msb = (byte)(r & 0x80);
        byte result = (byte)(r << 1);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = msb != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int SLA_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte msb = (byte)(val & 0x80);
        byte result = (byte)(val << 1);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = msb != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int SRA_R(ref byte r) {
        byte msb = (byte)(r & 0x80);
        byte lsb = (byte)(r & 0x01);
        byte result = (byte)((r >> 1) | msb);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = lsb != 0;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int SRA_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte msb = (byte)(val & 0x80);
        byte lsb = (byte)(val & 0x01);
        byte result = (byte)((val >> 1) | msb);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = lsb != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int SRL_R(ref byte r) {
        byte lsb = (byte)(r & 0x01);
        byte result = (byte)(r >> 1);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = lsb != 0;
        UpdateFFromFlags();

        r = result;
        
        return 8;
    }

    private int SRL_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte lsb = (byte)(val & 0x01);
        byte result = (byte)(val >> 1);

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = lsb != 0;
        UpdateFFromFlags();

        mmu.Write(address, result);
        
        return 16;
    }

    private int SWAP_R(ref byte r) {
        byte upper = (byte)(r & 0xF0);
        byte lower = (byte)(r & 0x0F);
        byte result = (byte)((lower << 4) | (upper >> 4));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        r = result;

        return 8;
    }

    private int SWAP_AHL() {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte upper = (byte)(val & 0xF0);
        byte lower = (byte)(val & 0x0F);
        byte result = (byte)((lower << 4) | (upper >> 4));

        zero = result == 0;
        negative = false;
        halfCarry = false;
        carry = false;
        UpdateFFromFlags();

        mmu.Write(address, result);

        return 16;
    }

    private int BIT_N_R(byte n, ref byte r) {
        byte bitTest = (byte)(r & (1 << n));

        zero = bitTest == 0;   
        negative = false;
        halfCarry = true;
        UpdateFFromFlags();

        return 8;
    }

    private int BIT_N_AHL(byte n) {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte bitTest = (byte)(val & (1 << n));

        zero = bitTest == 0;   
        negative = false;
        halfCarry = true;
        UpdateFFromFlags();

        return 12;
    }

    private int RES_N_R(byte n, ref byte r) {
        byte mask = (byte)~(1 << n);
        r &= mask;
        
        return 8;
    }

    private int RES_N_AHL(byte n) {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte mask = (byte)~(1 << n);
        val &= mask;

        mmu.Write(address, val);
        
        return 16;
    }

    private int SET_N_R(byte n, ref byte r) {
        byte mask = (byte)(1 << n);
        r |= mask;
        
        return 8;
    }

    private int SET_N_AHL(byte n) {
        ushort address = Get16BitReg("hl");
        byte val = mmu.Read(address);
        byte mask = (byte)(1 << n);
        val |= mask;

        mmu.Write(address, val);
        
        return 16;
    }

    //control
    private int JR_CON_I8(bool flag) {
        byte rel = Fetch();

        if (flag) {
            sbyte offset = (sbyte)rel;
            PC = (ushort)(PC + offset);
            return 12;
        } else {
            return 8;
        }
    }

    private int CALL_U16() {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort address = (ushort)((upper << 8) | lower);

        ushort returnAddress = (ushort)(PC);
        SP--; 
        mmu.Write(SP, (byte)(returnAddress >> 8));
        SP--; 
        mmu.Write(SP, (byte)(returnAddress & 0xFF));

        PC = address;
        return 24;
    }

    private int CALL_CON_U16(bool flag) {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort address = (ushort)((upper << 8) | lower);

        if (flag) {
            ushort returnAddress = (ushort)(PC);
            SP--; 
            mmu.Write(SP, (byte)(returnAddress >> 8));
            SP--; 
            mmu.Write(SP, (byte)(returnAddress & 0xFF));

            PC = address;
            return 24;
        } else {
            return 12;
        }
    }

    private int RET() {
        byte lowByte = mmu.Read(SP);
        SP++;
        byte highByte = mmu.Read(SP);
        SP++;

        ushort returnAddress = (ushort)((highByte << 8) | lowByte);

        PC = returnAddress;
        return 16;
    }

    private int RET_CON(bool flag) {
        if (flag) {
            byte lowByte = mmu.Read(SP);
            SP++;
            byte highByte = mmu.Read(SP);
            SP++;

            ushort returnAddress = (ushort)((highByte << 8) | lowByte);

            PC = returnAddress;
            return 20;
        } else {
            return 8;
        }
    }

    private int RETI() {
        IME = true;

        byte lowByte = mmu.Read(SP);
        SP++;
        byte highByte = mmu.Read(SP);
        SP++;

        ushort returnAddress = (ushort)((highByte << 8) | lowByte);

        PC = returnAddress;
        return 16;
    }

    private int JP_CON_U16(bool flag) {
        byte lower = Fetch();
        byte upper = Fetch();
        ushort address = (ushort)((upper << 8) | lower);

        if (flag) {
            PC = address;
            return 16;
        } else {
            return 12;
        }
    }

    private int JP_HL() {
        ushort address = Get16BitReg("hl");
        PC = address;
        return 4;
    }

    private int RST(ushort vector) {
        ushort returnAddress = (ushort)(PC);
        SP--; 
        mmu.Write(SP, (byte)(returnAddress >> 8));
        SP--; 
        mmu.Write(SP, (byte)(returnAddress & 0xFF));

        PC = vector;
        return 16;
    }

    //misc
    private int NOP() {
        return 4;
    }

    private int DI() {
        IME = false;
        return 4;
    }

    private int EI() {
        IME = true;
        return 4;
    }

    private int HALT() {
        halted = true;
        return 4;
    }

    private int STOP() {
        return 4;
    }

    private int DMG_EXIT(byte op) {
        Console.WriteLine("Unimplemented Opcode: " + op.ToString("X2") + " , PC: " + (PC-1).ToString("X4") + " (In opcode switch)");
        Environment.Exit(1);
        return 0;
    }
}